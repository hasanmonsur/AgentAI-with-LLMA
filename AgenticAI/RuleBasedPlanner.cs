using System.Text.Json.Nodes;

namespace AgenticAI;

/// <summary>
/// A rule-based planner that maps goal keywords to step sequences.
/// In production, replace with an LLM call (GPT-4, Claude, etc.) that
/// returns a structured plan as JSON.
/// </summary>
public sealed class RuleBasedPlanner : IPlanner
{
    private readonly ToolRegistry _registry;

    public RuleBasedPlanner(ToolRegistry registry) => _registry = registry;

    public Task<AgentPlan> CreatePlanAsync(
        string goal,
        IReadOnlyDictionary<string, object> context,
        CancellationToken ct = default)
    {
        var steps = InferSteps(goal, context);
        return Task.FromResult(new AgentPlan(goal, steps));
    }

    private List<AgentStep> InferSteps(string goal, IReadOnlyDictionary<string, object> ctx)
    {
        if (string.IsNullOrWhiteSpace(goal))
            return new List<AgentStep>();

        var g = goal.ToLowerInvariant();
        var steps = new List<AgentStep>();

        // ── Research & summarize pattern ──────────────────────────────────────
        if (Contains(g, "research", "find", "search", "look up", "summarize"))
        {
            steps.Add(Step("search", "Search the web for relevant information",
                "web_search", Params("query", goal), Array.Empty<string>()));

            if (Contains(g, "page", "url", "site", "http", "fetch", "docs"))
                steps.Add(Step("fetch", "Fetch content from a source URL",
                    "http_fetch", Params("url", "https://example.com"), new[] { "search" }));

            steps.Add(Step("summarize", "Analyze results and produce summary",
                "file", WriteParams("summary.md", $"# Summary\n\nGoal: {goal}\n\n[Analysis complete]"), new[] { "search" }));
        }

        // ── Calculation pattern ───────────────────────────────────────────────
        else if (Contains(g, "calculate", "compute", "math", "formula"))
        {
            var expr = ExtractExpr(goal);
            steps.Add(Step("calc", "Evaluate the math expression",
                "calculator", Params("expression", expr), Array.Empty<string>()));
            steps.Add(Step("save", "Save result to file",
                "file", WriteParams("calculation.txt", $"Result of: {goal}"), new[] { "calc" }));
        }

        // ── File operations pattern ───────────────────────────────────────────
        else if (Contains(g, "write", "create", "generate", "save", "output"))
        {
            steps.Add(Step("draft", "Generate initial content",
                "code_exec", Params("code", "Console.WriteLine(\"Agent-generated content\");"), Array.Empty<string>()));
            steps.Add(Step("save", "Persist content to file",
                "file", WriteParams("output.md", $"# Output\n\nGoal: {goal}"), new[] { "draft" }));
        }

        // ── Fallback: single search step ─────────────────────────────────────
        else
        {
            steps.Add(Step("search", "Search for relevant information",
                "web_search", Params("query", goal), Array.Empty<string>()));
        }

        return steps;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static AgentStep Step(
        string id, string desc, string tool,
        JsonObject p, IReadOnlyList<string>? deps = null) =>
        new(id, desc, tool, p, deps ?? Array.Empty<string>());

    private static JsonObject Params(string key, string value) =>
        new() { [key] = JsonValue.Create(value) };

    private static JsonObject WriteParams(string path, string content) =>
        new()
        {
            ["action"] = "write",
            ["path"] = path,
            ["content"] = content
        };

    private static bool Contains(string text, params string[] keywords) =>
        keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));

    private static string ExtractExpr(string goal)
    {
        // Very naive — extracts anything that looks like a math expression
        var parts = goal.Split(' ');
        foreach (var p in parts)
            if (p.Any(c => char.IsDigit(c) || c is '+' or '-' or '*' or '/' or '(' or ')'))
                return p;
        return "1+1";
    }
}
