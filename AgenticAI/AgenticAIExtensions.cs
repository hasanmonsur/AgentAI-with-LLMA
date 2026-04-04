using Microsoft.Extensions.DependencyInjection;

namespace AgenticAI;

// ─── DI Extension ──────────────────────────────────────────────────────────────

public static class AgenticAIServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Agentic AI framework into the DI container.
    /// </summary>
    public static IServiceCollection AddAgenticAI(
        this IServiceCollection services,
        Action<AgentBuilder>? configure = null)
    {
        var builder = new AgentBuilder();
        configure?.Invoke(builder);

        services.AddSingleton<IMemoryStore>(builder.MemoryStore ?? new InMemoryStore());
        services.AddSingleton(builder.Registry);
        services.AddSingleton<IPlanner, RuleBasedPlanner>();
        services.AddSingleton<IAgentOrchestrator>(sp => new AgentOrchestrator(
            sp.GetRequiredService<IPlanner>(),
            sp.GetRequiredService<ToolRegistry>(),
            sp.GetRequiredService<IMemoryStore>(),
            builder.Options
        ));

        return services;
    }
}

// ─── Fluent Builder ────────────────────────────────────────────────────────────

public sealed class AgentBuilder
{
    internal ToolRegistry Registry { get; } = new();
    internal IMemoryStore? MemoryStore { get; private set; }
    internal AgentOptions Options { get; private set; } = new();

    public AgentBuilder WithTool(IAgentTool tool)
    {
        Registry.Register(tool);
        return this;
    }

    public AgentBuilder WithDefaultTools(string fileOutputDir = "agent_output")
    {
        Registry.Register(new WebSearchTool());
        Registry.Register(new HttpFetchTool());
        Registry.Register(new FileTool(fileOutputDir));
        Registry.Register(new CalculatorTool());
        return this;
    }

    public AgentBuilder WithMemory(IMemoryStore store)
    {
        MemoryStore = store;
        return this;
    }

    public AgentBuilder WithOptions(AgentOptions options)
    {
        Options = options;
        return this;
    }
}
