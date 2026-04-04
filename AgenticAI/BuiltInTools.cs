using System.Diagnostics;
using System.Text.Json.Nodes;

namespace AgenticAI;

// ─── Web Search Tool ───────────────────────────────────────────────────────────

/// <summary>
/// Simulates a web search. In production, plug in Bing/Brave/Serper API.
/// </summary>
public sealed class WebSearchTool : AgentToolBase
{
    public override string Name => "web_search";
    public override string Description => "Search the web for information. Params: { query: string, max_results?: int }";

    protected override async Task<ToolResult> RunAsync(JsonObject p, CancellationToken ct)
    {
        var query = RequireParam(p, "query");
        var maxResults = OptionalParam<int>(p, "max_results", 5);

        await Task.Delay(200, ct); // simulate latency

        // In production: call Bing Search API, Serper, Brave, etc.
        var results = SimulatedSearch(query, maxResults);
        return ToolResult.Ok(results);
    }

    private static string SimulatedSearch(string query, int max)
    {
        // Returns plausible-looking results for demo purposes.
        var results = new[]
        {
            $"[1] learn.microsoft.com — .NET 9 Release Notes: {query}",
            $"[2] devblogs.microsoft.com — What's new in {query}",
            $"[3] github.com/dotnet — Runtime improvements for {query}",
            $"[4] nuget.org — Related packages: {query.Split(' ').First()}",
            $"[5] stackoverflow.com — Q&A: How to use {query}",
        };
        return string.Join("\n", results.Take(max));
    }
}

// ─── HTTP Fetch Tool ───────────────────────────────────────────────────────────

public sealed class HttpFetchTool : AgentToolBase
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public override string Name => "http_fetch";
    public override string Description => "Fetch content from a URL. Params: { url: string, max_length?: int }";

    protected override async Task<ToolResult> RunAsync(JsonObject p, CancellationToken ct)
    {
        var url = RequireParam(p, "url");
        var maxLength = OptionalParam<int>(p, "max_length", 4000);

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            return ToolResult.Fail($"Invalid URL: {url}");

        try
        {
            var content = await _http.GetStringAsync(url, ct);
            var truncated = content.Length > maxLength
                ? content[..maxLength] + $"\n... [truncated at {maxLength} chars]"
                : content;
            return ToolResult.Ok($"Fetched {content.Length} chars from {url}\n\n{truncated}");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult.Fail($"HTTP error fetching {url}: {ex.Message}");
        }
    }
}

// ─── Code Execution Tool ──────────────────────────────────────────────────────

/// <summary>
/// Executes a C# script using dotnet-script or csc. Requires dotnet on PATH.
/// </summary>
public sealed class CodeExecTool : AgentToolBase
{
    public override string Name => "code_exec";
    public override string Description => "Execute C# code and return output. Params: { code: string, language?: string }";

    protected override async Task<ToolResult> RunAsync(JsonObject p, CancellationToken ct)
    {
        var code = RequireParam(p, "code");
        var lang = OptionalParam<string>(p, "language", "csharp");

        if (lang != "csharp")
            return ToolResult.Fail($"Language '{lang}' not supported. Use 'csharp'.");

        // Write temp script file and run via dotnet-script (dotnet tool install -g dotnet-script)
        var tempFile = Path.Combine(Path.GetTempPath(), $"agent_{Guid.NewGuid():N}.csx");
        await File.WriteAllTextAsync(tempFile, code, ct);

        try
        {
            var psi = new ProcessStartInfo("dotnet-script", tempFile)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start process.");
            var outputTask = proc.StandardOutput.ReadToEndAsync(ct);
            var errorTask = proc.StandardError.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);

            var output = await outputTask;
            var error = await errorTask;

            return proc.ExitCode == 0
                ? ToolResult.Ok(output.Trim())
                : ToolResult.Fail($"Exit {proc.ExitCode}: {error.Trim()}");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}

// ─── File Tool ────────────────────────────────────────────────────────────────

public sealed class FileTool : AgentToolBase
{
    private readonly string _rootDir;

    public FileTool(string rootDir = "agent_output")
    {
        _rootDir = rootDir;
        Directory.CreateDirectory(_rootDir);
    }

    public override string Name => "file";
    public override string Description => "Read or write files. Params: { action: 'read'|'write'|'list', path?: string, content?: string }";

    protected override async Task<ToolResult> RunAsync(JsonObject p, CancellationToken ct)
    {
        var action = RequireParam(p, "action");
        return action.ToLower() switch
        {
            "write" => await WriteAsync(p, ct),
            "read"  => await ReadAsync(p, ct),
            "list"  => ListDir(),
            _ => ToolResult.Fail($"Unknown action: '{action}'. Use read, write, or list.")
        };
    }

    private async Task<ToolResult> WriteAsync(JsonObject p, CancellationToken ct)
    {
        var path = Path.Combine(_rootDir, RequireParam(p, "path"));
        var content = RequireParam(p, "content");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content, ct);
        return ToolResult.Ok($"Written {content.Length} chars to {path}");
    }

    private async Task<ToolResult> ReadAsync(JsonObject p, CancellationToken ct)
    {
        var path = Path.Combine(_rootDir, RequireParam(p, "path"));
        if (!File.Exists(path)) return ToolResult.Fail($"File not found: {path}");
        var content = await File.ReadAllTextAsync(path, ct);
        return ToolResult.Ok(content);
    }

    private ToolResult ListDir()
    {
        var files = Directory.GetFiles(_rootDir, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(_rootDir, f));
        return ToolResult.Ok(string.Join("\n", files));
    }
}

// ─── Calculator Tool ──────────────────────────────────────────────────────────

public sealed class CalculatorTool : AgentToolBase
{
    public override string Name => "calculator";
    public override string Description => "Evaluate a math expression. Params: { expression: string }";

    protected override Task<ToolResult> RunAsync(JsonObject p, CancellationToken ct)
    {
        var expr = RequireParam(p, "expression");
        try
        {
            // Safe evaluator — only allows numeric expressions
            var result = EvaluateSafe(expr);
            return Task.FromResult(ToolResult.Ok($"{expr} = {result}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Could not evaluate '{expr}': {ex.Message}"));
        }
    }

    private static double EvaluateSafe(string expr)
    {
        // Uses DataTable.Compute as a safe numeric evaluator (no code injection)
        var dt = new System.Data.DataTable();
        var result = dt.Compute(expr, null);
        return Convert.ToDouble(result);
    }
}
