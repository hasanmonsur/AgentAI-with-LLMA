using AgenticAI;
using WebApiTest;

var builder = WebApplication.CreateBuilder(args);

// Configure AgenticAI framework
builder.Services.AddAgenticAI(agent =>
    agent
        .WithDefaultTools("wwwroot/outputs")
        .WithTool(new SlackTool(builder.Configuration["Slack:Token"] ?? "demo-token"))
        .WithTool(new SqlQueryTool(builder.Configuration.GetConnectionString("Default") ?? "Server=localhost;Database=demo"))
        .WithOptions(new AgentOptions { MaxRetries = 3, MaxIterations = 25 })
);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();


// Endpoint: Run an agent goal
app.MapPost("/agent/run", async (AgentRequest req, IAgentOrchestrator agent) =>
{
    if (string.IsNullOrWhiteSpace(req.Goal))
        return Results.BadRequest(new { error = "Goal cannot be empty" });

    try
    {
        var result = await agent.RunAsync(req.Goal);
        return Results.Ok(new
        {
            success = result.Success,
            output = result.Output,
            stepsExecuted = result.StepsExecuted,
            toolCallsMade = result.ToolCallsMade,
            retries = result.Retries
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 500);
    }
})
.WithName("RunAgent");

app.Run();


/// <summary>
/// Request model for running an agent goal.
/// </summary>
internal record AgentRequest(string Goal)
{
    public AgentRequest() : this(string.Empty) { }
};