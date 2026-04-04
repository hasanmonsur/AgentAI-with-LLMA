using System.Text.Json.Nodes;
using AgenticAI;

namespace WebApiTest;

/// <summary>
/// SQL query execution tool for database operations.
/// In production, use parameterized queries and execute against your database.
/// </summary>
public sealed class SqlQueryTool : AgentToolBase
{
    private readonly string _connectionString;

    public SqlQueryTool(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public override string Name => "sql_query";
    public override string Description => "Execute SQL queries. Params: { query: string, max_rows?: int }";

    protected override async Task<ToolResult> RunAsync(JsonObject p, CancellationToken ct)
    {
        try
        {
            var query = RequireParam(p, "query");
            var maxRows = OptionalParam<int>(p, "max_rows", 10);

            // Validate connection string
            if (string.IsNullOrWhiteSpace(_connectionString))
                return ToolResult.Fail("Database connection string is not configured.");

            // Security: Only allow SELECT queries in this demo
            var trimmedQuery = query.Trim();
            if (!trimmedQuery.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return ToolResult.Fail("Only SELECT queries are allowed for security reasons.");

            // Simulate database query execution
            await Task.Delay(150, ct);

            // In production, execute actual query:
            // using (var connection = new SqlConnection(_connectionString))
            // {
            //     var command = new SqlCommand(query, connection);
            //     // Execute and retrieve results
            // }

            var result = $"Query executed successfully.\n" +
                        $"Query: {query.Substring(0, Math.Min(100, query.Length))}...\n" +
                        $"Rows returned: ~{maxRows} (simulated)";

            return ToolResult.Ok(result);
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"SQL query tool error: {ex.Message}");
        }
    }
}
