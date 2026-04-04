using AgenticAI;
using Microsoft.Extensions.DependencyInjection;

// ═══════════════════════════════════════════════════════════════════════════════
//  Agentic AI Framework — .NET Core Demo
//  Three patterns: (1) Quick factory, (2) Full DI, (3) Custom tool
// ═══════════════════════════════════════════════════════════════════════════════

Console.WriteLine("╔══════════════════════════════════════════════╗");
Console.WriteLine("║   Agentic AI Framework  —  .NET Core Demo   ║");
Console.WriteLine("╚══════════════════════════════════════════════╝\n");

// ─── Pattern 1: Quick factory setup ───────────────────────────────────────────

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("▶ PATTERN 1 — Quick factory + research goal");
Console.ResetColor();

var registry = new ToolRegistry();
registry.Register(new WebSearchTool());
registry.Register(new HttpFetchTool());
registry.Register(new FileTool());
registry.Register(new CalculatorTool());

var memory     = new InMemoryStore();
var planner    = new RuleBasedPlanner(registry);
var orchestrator = new AgentOrchestrator(planner, registry, memory);

// Subscribe to real-time events
orchestrator.OnEvent += (_, e) =>
{
    var color = e.Type switch
    {
        AgentEventType.PlanCreated  => ConsoleColor.Yellow,
        AgentEventType.ToolCalled   => ConsoleColor.Magenta,
        AgentEventType.StepCompleted=> ConsoleColor.Green,
        AgentEventType.StepFailed   => ConsoleColor.Red,
        AgentEventType.MemoryUpdated=> ConsoleColor.DarkCyan,
        AgentEventType.GoalCompleted=> ConsoleColor.Cyan,
        _ => ConsoleColor.Gray
    };
    Console.ForegroundColor = color;
    Console.WriteLine($"  [{e.Type,-14}] {e.Message}");
    Console.ResetColor();
};

var result1 = await orchestrator.RunAsync(
    "Research .NET 9 performance improvements and summarize key findings");

Console.WriteLine();
Console.WriteLine("┌─ Execution Summary ─────────────────────────────────────┐");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"│ Status: COMPLETED");
Console.WriteLine($"│ Steps Executed: {result1.StepsExecuted}");
Console.WriteLine($"│ Tool Calls Made: {result1.ToolCallsMade}");
Console.WriteLine($"│ Retries: {result1.Retries}");
Console.ResetColor();
Console.WriteLine("└─────────────────────────────────────────────────────────┘");
Console.WriteLine();
Console.WriteLine("📝 Agent Output:");
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine(result1.Output);
Console.ResetColor();
Console.WriteLine();
Console.WriteLine("💾 Memory State:");
Console.WriteLine($"{memory}\n");

// ─── Pattern 2: DI container wiring ───────────────────────────────────────────

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("▶ PATTERN 2 — Microsoft DI container");
Console.ResetColor();

var services = new ServiceCollection();
services.AddAgenticAI(builder =>
    builder
        .WithDefaultTools("output")
        .WithOptions(new AgentOptions { MaxRetries = 2, MaxIterations = 20 })
);

var sp = services.BuildServiceProvider();
var agent2 = sp.GetRequiredService<IAgentOrchestrator>();
agent2.OnEvent += (_, e) => Console.WriteLine($"  [{e.Type,-14}] {e.Message}");

Console.WriteLine("\n📌 Running calculation task via DI container...\n");
var result2 = await agent2.RunAsync("Calculate 2^16 + 1024 * 3");
Console.WriteLine();
Console.WriteLine("┌─ Pattern 2 Result ──────────────────────────────────────┐");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"│ Calculation Result: {result2.Output.Trim()}");
Console.ResetColor();
Console.WriteLine("└─────────────────────────────────────────────────────────┘\n");

// ─── Pattern 3: Custom tool ────────────────────────────────────────────────────

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("▶ PATTERN 3 — Registering a custom IAgentTool");
Console.ResetColor();

registry.Register(new WeatherTool());

Console.WriteLine("\n📌 Testing custom weather tool...\n");
var result3 = await orchestrator.RunAsync("Search for weather in London and summarize");
Console.WriteLine();
Console.WriteLine("┌─ Pattern 3 Result ──────────────────────────────────────┐");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"│ Custom Tool Execution: {(result3.Success ? "SUCCESS" : "FAILED")}");
Console.WriteLine($"│ Output:");
Console.ResetColor();
Console.WriteLine($"│ {result3.Output}");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("└─────────────────────────────────────────────────────────┘\n");
Console.ResetColor();

Console.WriteLine("All patterns complete.");

// ─── Summary ───────────────────────────────────────────────────────────────────

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("\n╔═════════════════════════════════════════════════════════╗");
Console.WriteLine("║         AGENTIC AI FRAMEWORK DEMO COMPLETE             ║");
Console.WriteLine("╚═════════════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine("\n✓ Pattern 1: Research task with web search & memory tracking");
Console.WriteLine("✓ Pattern 2: Dependency injection container integration");
Console.WriteLine("✓ Pattern 3: Custom tool registration & execution\n");

// ─── Custom tool example ───────────────────────────────────────────────────────

/// <summary>
/// Example of a custom IAgentTool. Register any external API the same way.
/// </summary>
sealed class WeatherTool : AgentToolBase
{
    public override string Name => "weather";
    public override string Description => "Get weather for a city. Params: { city: string }";

    protected override async Task<ToolResult> RunAsync(
        System.Text.Json.Nodes.JsonObject p,
        CancellationToken ct)
    {
        try
        {
            var city = RequireParam(p, "city");

            // Simulate API call
            await Task.Delay(100, ct);

            // Generate comprehensive weather data
            var weatherData = new
            {
                City = city,
                Country = "UK",
                Temperature = 14,
                FeelsLike = 12,
                Condition = "Partly Cloudy",
                Humidity = 72,
                WindSpeed = 12,
                Pressure = 1013,
                Visibility = 10,
                UVIndex = 3,
                Sunrise = "07:15 AM",
                Sunset = "04:45 PM",
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var output = $@"
╔════════════════════════════════════════════╗
║  WEATHER INFORMATION FOR {city.ToUpper()}
╚════════════════════════════════════════════╝
📍 Location: {weatherData.City}, {weatherData.Country}
🌡️  Temperature: {weatherData.Temperature}°C (feels like {weatherData.FeelsLike}°C)
☁️  Condition: {weatherData.Condition}
💧 Humidity: {weatherData.Humidity}%
💨 Wind Speed: {weatherData.WindSpeed} km/h
🔽 Pressure: {weatherData.Pressure} hPa
👁️  Visibility: {weatherData.Visibility} km
☀️  UV Index: {weatherData.UVIndex}
🌅 Sunrise: {weatherData.Sunrise}
🌇 Sunset: {weatherData.Sunset}
⏰ Updated: {weatherData.LastUpdated}
";

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("  [ToolCalled] WeatherTool executed successfully");
            Console.ResetColor();

            return ToolResult.Ok(output);
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"Weather tool error: {ex.Message}");
        }
    }
}





