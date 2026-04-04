# Agentic AI Framework — .NET Core

A production-ready agentic AI framework for .NET 9 implementing the **ReAct** (Reason + Act) loop pattern with pluggable tools, memory, and planning.

## Architecture

```
Goal (string)
  │
  ▼
┌──────────────────────────────────────────────────┐
│              AgentOrchestrator                   │
│  ┌──────────┐  ┌──────────┐  ┌───────────────┐  │
│  │ IPlanner │  │ToolReg.  │  │ IMemoryStore  │  │
│  │ (Plan)   │→ │ (Act)    │→ │ (Observe)     │  │
│  └──────────┘  └──────────┘  └───────────────┘  │
│                    ↑                              │
│            ┌───────┴────────┐                    │
│            │  IAgentTool[]  │                    │
│            │ WebSearch      │                    │
│            │ HttpFetch      │                    │
│            │ CodeExec       │                    │
│            │ File           │                    │
│            │ Calculator     │                    │
│            │ + your own     │                    │
│            └────────────────┘                    │
└──────────────────────────────────────────────────┘
  │
  ▼
AgentResult { Success, Output, StepsExecuted, ToolCallsMade, Retries }
```

## Quick start

```bash
dotnet run --project AgenticAI/AgenticAI.csproj
```

## Usage patterns

### Pattern 1 — Direct instantiation

```csharp
var registry = new ToolRegistry();
registry.Register(new WebSearchTool());
registry.Register(new FileTool());

var agent = new AgentOrchestrator(
    new RuleBasedPlanner(registry),
    registry,
    new InMemoryStore()
);

agent.OnEvent += (_, e) => Console.WriteLine(e.Message);

var result = await agent.RunAsync("Research .NET 9 features and summarize");
Console.WriteLine(result.Output);
```

### Pattern 2 — Microsoft DI

```csharp
services.AddAgenticAI(builder =>
    builder
        .WithDefaultTools("output")
        .WithOptions(new AgentOptions { MaxRetries = 3 })
);

var agent = serviceProvider.GetRequiredService<IAgentOrchestrator>();
var result = await agent.RunAsync(goal);
```

### Pattern 3 — Custom tool

```csharp
public sealed class SlackTool : AgentToolBase
{
    public override string Name => "slack_send";
    public override string Description => "Send a Slack message. Params: { channel, message }";

    protected override async Task<ToolResult> RunAsync(JsonObject p, CancellationToken ct)
    {
        var channel = RequireParam(p, "channel");
        var message = RequireParam(p, "message");
        // call Slack API...
        return ToolResult.Ok($"Message sent to {channel}");
    }
}

registry.Register(new SlackTool());
```

## Extending the Planner

Swap `RuleBasedPlanner` with an LLM-powered planner:

```csharp
public sealed class ClaudePlanner : IPlanner
{
    private readonly AnthropicClient _client;

    public async Task<AgentPlan> CreatePlanAsync(string goal, ...)
    {
        var response = await _client.Messages.CreateAsync(new()
        {
            Model = "claude-opus-4-5",
            System = $"You are a planner. Available tools:\n{_registry.Describe()}",
            Messages = [new("user", $"Create a step-by-step plan for: {goal}. Return JSON.")]
        });

        return ParsePlan(response.Content[0].Text);
    }
}
```

## ReAct Loop

Each agent run follows:

1. **Reason** — Planner decomposes the goal into ordered steps with tool assignments
2. **Act** — Orchestrator calls the appropriate tool for each step
3. **Observe** — Tool result is stored in `IMemoryStore`
4. **Reflect** — On failure, retry up to `MaxRetries` with exponential backoff
5. **Complete** — Aggregate outputs into a final `AgentResult`

## Key interfaces

| Interface | Purpose |
|---|---|
| `IAgentTool` | Implement to add a new tool |
| `IMemoryStore` | Swap for Redis, SQLite, Cosmos DB |
| `IPlanner` | Swap for LLM-based planning |
| `IAgentOrchestrator` | Override the full ReAct loop |

## Production tips

- Replace `InMemoryStore` with `RedisMemoryStore` for distributed agents
- Replace `RuleBasedPlanner` with an LLM call for dynamic task decomposition
- Add `IAgentMiddleware` around `AgentOrchestrator` for logging, auth, rate limiting
- Use `CancellationToken` propagation for graceful timeout handling
- Emit `AgentEvent` to OpenTelemetry spans for observability
