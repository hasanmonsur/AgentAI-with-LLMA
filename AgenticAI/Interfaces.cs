using System.Text.Json.Nodes;

namespace AgenticAI;

// ─── Tool abstraction ──────────────────────────────────────────────────────────

public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    Task<ToolResult> ExecuteAsync(JsonObject parameters, CancellationToken ct = default);
}

public record ToolResult(bool Success, string Output, string? Error = null)
{
    public static ToolResult Ok(string output) => new(true, output);
    public static ToolResult Fail(string error) => new(false, string.Empty, error);
}

// ─── Memory abstraction ────────────────────────────────────────────────────────

public interface IMemoryStore
{
    void Set(string key, object value);
    T? Get<T>(string key);
    bool TryGet<T>(string key, out T? value);
    IReadOnlyDictionary<string, object> GetAll();
    void Clear();
}

// ─── Planner abstraction ───────────────────────────────────────────────────────

public interface IPlanner
{
    Task<AgentPlan> CreatePlanAsync(string goal, IReadOnlyDictionary<string, object> context, CancellationToken ct = default);
}

public record AgentPlan(string Goal, IReadOnlyList<AgentStep> Steps);

public record AgentStep(
    string Id,
    string Description,
    string ToolName,
    JsonObject Parameters,
    IReadOnlyList<string> DependsOn
);

// ─── Orchestrator abstraction ──────────────────────────────────────────────────

public interface IAgentOrchestrator
{
    Task<AgentResult> RunAsync(string goal, CancellationToken ct = default);
    event EventHandler<AgentEvent>? OnEvent;
}

public record AgentResult(bool Success, string Output, int StepsExecuted, int ToolCallsMade, int Retries);

public record AgentEvent(AgentEventType Type, string Message, string? StepId = null);

public enum AgentEventType { PlanCreated, StepStarted, StepCompleted, StepFailed, ToolCalled, MemoryUpdated, GoalCompleted }
