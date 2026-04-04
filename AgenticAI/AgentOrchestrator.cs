namespace AgenticAI;

/// <summary>
/// The main agentic orchestrator.
///
/// Implements a ReAct-style loop:
///   1. Plan  — decompose goal into ordered steps
///   2. Act   — call a tool for each step
///   3. Observe — inspect result, update memory
///   4. Reflect — retry on failure, skip on success
///   5. Complete — aggregate results and return
/// </summary>
public sealed class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IPlanner _planner;
    private readonly ToolRegistry _tools;
    private readonly IMemoryStore _memory;
    private readonly AgentOptions _options;

    public event EventHandler<AgentEvent>? OnEvent;

    public AgentOrchestrator(
        IPlanner planner,
        ToolRegistry tools,
        IMemoryStore memory,
        AgentOptions? options = null)
    {
        _planner = planner;
        _tools = tools;
        _memory = memory;
        _options = options ?? new AgentOptions();
    }

    public async Task<AgentResult> RunAsync(string goal, CancellationToken ct = default)
    {
        _memory.Clear();
        _memory.Set("goal", goal);

        Emit(AgentEventType.PlanCreated, $"Starting goal: {goal}");

        // ── Step 1: Plan ──────────────────────────────────────────────────────
        var context = _memory.GetAll();
        var plan = await _planner.CreatePlanAsync(goal, context, ct);
        Emit(AgentEventType.PlanCreated, $"Plan created with {plan.Steps.Count} step(s)");
        _memory.Set("plan", plan);

        var completed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var outputs   = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int toolCalls = 0;
        int retries   = 0;
        var remaining = new Queue<AgentStep>(plan.Steps);

        // ── Step 2-4: ReAct loop ──────────────────────────────────────────────
        int iteration = 0;
        while (remaining.Count > 0 && iteration++ < _options.MaxIterations)
        {
            ct.ThrowIfCancellationRequested();

            var step = remaining.Dequeue();

            // Check dependencies are satisfied
            if (step.DependsOn.Any(dep => !completed.Contains(dep)))
            {
                // Re-enqueue — deps not yet done
                remaining.Enqueue(step);
                await Task.Delay(10, ct);
                continue;
            }

            Emit(AgentEventType.StepStarted, $"[{step.Id}] {step.Description}", step.Id);

            if (!_tools.TryGet(step.ToolName, out var tool) || tool is null)
            {
                Emit(AgentEventType.StepFailed, $"[{step.Id}] Tool '{step.ToolName}' not found", step.Id);
                continue;
            }

            // Inject outputs from prior steps into parameters
            var enrichedParams = InjectContext(step, outputs);

            ToolResult result = ToolResult.Fail("Not executed");
            for (int attempt = 1; attempt <= _options.MaxRetries; attempt++)
            {
                Emit(AgentEventType.ToolCalled, $"→ {tool.Name}({enrichedParams})", step.Id);
                toolCalls++;

                result = await tool.ExecuteAsync(enrichedParams, ct);

                if (result.Success) break;

                retries++;
                Emit(AgentEventType.StepFailed, $"[{step.Id}] attempt {attempt} failed: {result.Error}", step.Id);

                if (attempt < _options.MaxRetries)
                    await Task.Delay(_options.RetryDelayMs * attempt, ct);
            }

            // ── Step 3: Observe ────────────────────────────────────────────────
            if (result.Success)
            {
                completed.Add(step.Id);
                outputs[step.Id] = result.Output;

                _memory.Set($"step.{step.Id}.output", result.Output);
                Emit(AgentEventType.MemoryUpdated, $"Stored output for step '{step.Id}'", step.Id);
                Emit(AgentEventType.StepCompleted, $"[{step.Id}] completed → {Snippet(result.Output)}", step.Id);
            }
            else
            {
                // Step permanently failed — record and continue
                _memory.Set($"step.{step.Id}.error", result.Error ?? "Unknown error");
            }
        }

        // ── Step 5: Aggregate ─────────────────────────────────────────────────
        var finalOutput = BuildSummary(goal, outputs);
        _memory.Set("final_output", finalOutput);

        var success = completed.Count == plan.Steps.Count;
        Emit(AgentEventType.GoalCompleted,
            $"Done — {completed.Count}/{plan.Steps.Count} steps, {toolCalls} tool calls, {retries} retries");

        return new AgentResult(success, finalOutput, completed.Count, toolCalls, retries);
    }

    // ─── Private helpers ───────────────────────────────────────────────────────

    private static System.Text.Json.Nodes.JsonObject InjectContext(
        AgentStep step,
        Dictionary<string, string> outputs)
    {
        // Shallow clone + inject prior step outputs into parameters if referenced
        var copy = System.Text.Json.Nodes.JsonNode.Parse(step.Parameters.ToJsonString())!
            .AsObject();

        // Example injection: if a param value is "{step.id.output}" substitute it
        foreach (var key in copy.Select(kv => kv.Key).ToArray())
        {
            var raw = copy[key]?.GetValue<string>() ?? "";
            if (raw.StartsWith("{step.") && raw.EndsWith(".output}"))
            {
                var refId = raw[6..^8];
                if (outputs.TryGetValue(refId, out var injected))
                    copy[key] = injected;
            }
        }
        return copy;
    }

    private static string BuildSummary(string goal, Dictionary<string, string> outputs)
    {
        if (outputs.Count == 0) return $"Goal '{goal}' produced no output.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Goal: {goal}");
        sb.AppendLine();
        foreach (var (stepId, output) in outputs)
        {
            sb.AppendLine($"## {stepId}");
            sb.AppendLine(output.Length > 500 ? output[..500] + "..." : output);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string Snippet(string s, int max = 60) =>
        s.Length <= max ? s : s[..max] + "...";

    private void Emit(AgentEventType type, string msg, string? stepId = null) =>
        OnEvent?.Invoke(this, new AgentEvent(type, msg, stepId));
}

// ─── Options ───────────────────────────────────────────────────────────────────

public sealed class AgentOptions
{
    public int MaxIterations { get; init; } = 50;
    public int MaxRetries    { get; init; } = 3;
    public int RetryDelayMs  { get; init; } = 500;
}
