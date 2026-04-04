using System.Text.Json.Nodes;

namespace AgenticAI;

// ─── Tool Registry ─────────────────────────────────────────────────────────────

public sealed class ToolRegistry
{
    private readonly Dictionary<string, IAgentTool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IAgentTool tool) => _tools[tool.Name] = tool;

    public bool TryGet(string name, out IAgentTool? tool) => _tools.TryGetValue(name, out tool);

    public IReadOnlyCollection<IAgentTool> GetAll() => _tools.Values;

    public string Describe() => string.Join("\n", _tools.Values.Select(t => $"- {t.Name}: {t.Description}"));
}

// ─── Base tool helper ──────────────────────────────────────────────────────────

public abstract class AgentToolBase : IAgentTool
{
    public abstract string Name { get; }
    public abstract string Description { get; }

    public async Task<ToolResult> ExecuteAsync(JsonObject parameters, CancellationToken ct = default)
    {
        try
        {
            return await RunAsync(parameters, ct);
        }
        catch (OperationCanceledException)
        {
            return ToolResult.Fail("Operation was cancelled.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"Tool '{Name}' threw an exception: {ex.Message}");
        }
    }

    protected abstract Task<ToolResult> RunAsync(JsonObject parameters, CancellationToken ct);

    protected static string RequireParam(JsonObject p, string key)
    {
        var val = p[key]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(val))
            throw new ArgumentException($"Required parameter '{key}' is missing or empty.");
        return val;
    }

    protected static T? OptionalParam<T>(JsonObject p, string key, T? defaultValue = default)
    {
        try { return p[key] is JsonNode node ? node.GetValue<T>() : defaultValue; }
        catch { return defaultValue; }
    }
}
