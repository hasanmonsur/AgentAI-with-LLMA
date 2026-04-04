using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgenticAI;

/// <summary>
/// Factory for creating LLM-based planners.
/// Supports OpenAI, Azure OpenAI, and can be extended for other providers.
/// 
/// Usage:
///   var planner = await OpenAIPlannerFactory.CreateAsync(registry);
///   var orchestrator = new AgentOrchestrator(planner, registry, memory);
/// </summary>
public static class OpenAIPlannerFactory
{
    /// <summary>
    /// Creates an OpenAI-based planner using environment variables for configuration.
    /// 
    /// Environment variables:
    ///   - OPENAI_API_KEY: Your OpenAI API key
    ///   - OPENAI_MODEL: Model name (default: gpt-4-turbo)
    ///   - OPENAI_ENDPOINT: Optional Azure OpenAI endpoint
    /// </summary>
    public static async Task<IPlanner> CreateAsync(ToolRegistry registry)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException(
                "OPENAI_API_KEY environment variable not set. " +
                "Set your API key to use the LLM-based planner.");

        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4-turbo";
        var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT");

        // For production: would use Azure.AI.OpenAI client here
        // For demo: use basic HTTP client with OpenAI API
        return new OpenAIPlanner(registry, apiKey, model, endpoint);
    }

    /// <summary>
    /// Creates a mock planner for testing when no API key is available.
    /// Falls back to rule-based planning.
    /// </summary>
    public static IPlanner CreateMockAsync(ToolRegistry registry)
        => new RuleBasedPlanner(registry);
}

/// <summary>
/// LLM-based planner using OpenAI API.
/// Decomposes goals into structured plans via GPT-4.
/// </summary>
public sealed class OpenAIPlanner : IPlanner
{
    private readonly ToolRegistry _registry;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string? _endpoint;
    private readonly HttpClient _httpClient;

    public OpenAIPlanner(ToolRegistry registry, string apiKey, string model, string? endpoint = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _model = model ?? "gpt-4-turbo";
        _endpoint = endpoint;
        _httpClient = new HttpClient();
    }

    public async Task<AgentPlan> CreatePlanAsync(
        string goal,
        IReadOnlyDictionary<string, object> context,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(goal))
            return new AgentPlan(goal, Array.Empty<AgentStep>());

        try
        {
            var prompt = BuildPrompt(goal, context);
            var response = await CallLLMAsync(prompt, ct);
            var steps = ParsePlanResponse(response);

            return new AgentPlan(goal, steps);
        }
        catch (Exception ex)
        {
            // Fallback to rule-based planner on LLM failure
            Console.WriteLine($"[Warning] LLM planning failed: {ex.Message}. Falling back to rule-based planner.");
            var fallback = new RuleBasedPlanner(_registry);
            return await fallback.CreatePlanAsync(goal, context, ct);
        }
    }

    private string BuildPrompt(string goal, IReadOnlyDictionary<string, object> context)
    {
        var toolDescriptions = _registry.Describe();
        var contextStr = context.Any() 
            ? $"Context: {string.Join(", ", context.Select(kvp => $"{kvp.Key}={kvp.Value}"))}"
            : "";

        var prompt = $$"""
You are an AI agent planner. Your task is to decompose a goal into a detailed, step-by-step plan.

Available tools:
{{toolDescriptions}}

Goal: {{goal}}

{{contextStr}}

Return a JSON plan with this exact structure:
{
  "goal": "the original goal",
  "steps": [
    {
      "id": "step1",
      "description": "What this step does",
      "toolName": "tool_name",
      "parameters": {"key": "value"},
      "dependsOn": []
    },
    {
      "id": "step2",
      "description": "What this step does",
      "toolName": "another_tool",
      "parameters": {"key": "value"},
      "dependsOn": ["step1"]
    }
  ]
}

Rules:
1. Use only tools from the list above
2. Each step must have a unique id (step1, step2, etc.)
3. dependsOn is an array of step ids this step depends on (empty if no dependencies)
4. parameters should match what each tool expects
5. Return ONLY the JSON, no other text
""";

        return prompt;
    }

    private async Task<string> CallLLMAsync(string prompt, CancellationToken ct)
    {
        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            max_tokens = 2000
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json")
        };

        request.Headers.Add("Authorization", $"Bearer {_apiKey}");

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));
        var content = responseJson?["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Unexpected API response format");

        return content;
    }

    private static List<AgentStep> ParsePlanResponse(string jsonResponse)
    {
        var steps = new List<AgentStep>();

        try
        {
            // Try to extract JSON from response (in case it has extra text)
            var jsonStart = jsonResponse.IndexOf('{');
            var jsonEnd = jsonResponse.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = jsonResponse[jsonStart..(jsonEnd + 1)];
                var planJson = JsonNode.Parse(jsonStr);

                var stepsArray = planJson?["steps"]?.AsArray();
                if (stepsArray != null)
                {
                    foreach (var stepNode in stepsArray)
                    {
                        var step = ParseStep(stepNode);
                        if (step != null)
                            steps.Add(step);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse LLM plan response: {ex.Message}", ex);
        }

        return steps.Any() ? steps : throw new InvalidOperationException("LLM produced no valid steps");
    }

    private static AgentStep? ParseStep(JsonNode? stepNode)
    {
        if (stepNode == null)
            return null;

        var id = stepNode["id"]?.GetValue<string>();
        var description = stepNode["description"]?.GetValue<string>();
        var toolName = stepNode["toolName"]?.GetValue<string>();
        var parameters = (stepNode["parameters"] as JsonObject) ?? new JsonObject();
        var dependsOn = stepNode["dependsOn"]?.AsArray()
            ?.Select(x => x?.GetValue<string>() ?? "")
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList() ?? new List<string>();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(toolName))
            return null;

        return new AgentStep(
            id,
            description ?? "No description",
            toolName,
            parameters,
            dependsOn.AsReadOnly()
        );
    }
}

/// <summary>
/// Azure-specific planner factory (for Azure OpenAI).
/// Requires: Azure.AI.OpenAI NuGet package
/// </summary>
public static class AzureOpenAIPlannerFactory
{
    /// <summary>
    /// Creates an Azure OpenAI-based planner.
    /// 
    /// Environment variables:
    ///   - AZURE_OPENAI_ENDPOINT: Your Azure OpenAI endpoint URL
    ///   - AZURE_OPENAI_API_KEY: Your Azure OpenAI API key
    ///   - AZURE_OPENAI_DEPLOYMENT_NAME: Deployment name (e.g., "gpt-4-turbo")
    /// </summary>
    public static IPlanner Create(ToolRegistry registry)
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT not set");
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
            ?? throw new InvalidOperationException("AZURE_OPENAI_API_KEY not set");
        var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME")
            ?? "gpt-4-turbo";

        return new AzureOpenAIPlanner(registry, endpoint, apiKey, deploymentName);
    }
}

/// <summary>
/// Azure OpenAI-based planner.
/// Requires: Azure.AI.OpenAI NuGet package
/// </summary>
public sealed class AzureOpenAIPlanner : IPlanner
{
    private readonly ToolRegistry _registry;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deploymentName;

    public AzureOpenAIPlanner(ToolRegistry registry, string endpoint, string apiKey, string deploymentName)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _deploymentName = deploymentName ?? throw new ArgumentNullException(nameof(deploymentName));
    }

    public async Task<AgentPlan> CreatePlanAsync(
        string goal,
        IReadOnlyDictionary<string, object> context,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(goal))
            return new AgentPlan(goal, Array.Empty<AgentStep>());

        try
        {
            // This would use Azure.AI.OpenAI SDK
            // For now, delegate to the basic OpenAI planner
            var planner = new OpenAIPlanner(_registry, _apiKey, _deploymentName, _endpoint);
            return await planner.CreatePlanAsync(goal, context, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] Azure OpenAI planning failed: {ex.Message}. Falling back to rule-based planner.");
            var fallback = new RuleBasedPlanner(_registry);
            return await fallback.CreatePlanAsync(goal, context, ct);
        }
    }
}
