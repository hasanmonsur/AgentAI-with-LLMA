╔═══════════════════════════════════════════════════════════════════════════════╗
║                                                                               ║
║                    LLM-BASED PLANNER SETUP & USAGE GUIDE                     ║
║                Replace RuleBasedPlanner with OpenAI/Azure OpenAI             ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝


📋 OVERVIEW
═════════════════════════════════════════════════════════════════════════════════

You now have THREE planner implementations available:

1. RuleBasedPlanner (Default)
   - Hardcoded rules based on keywords
   - Fast, deterministic, no API calls
   - Best for: Development, testing, offline use

2. OpenAIPlannerFactory (LLM-based)
   - Uses OpenAI API (GPT-4, GPT-3.5)
   - Supports custom prompts and reasoning
   - Best for: Production, complex tasks, dynamic planning

3. AzureOpenAIPlannerFactory (Azure LLM-based)
   - Uses Azure OpenAI endpoint
   - Enterprise-grade, VPC support
   - Best for: Enterprise, compliance, security


🚀 QUICK START - USE OPENAI PLANNER
═════════════════════════════════════════════════════════════════════════════════

STEP 1: Set your OpenAI API key

  Windows PowerShell:
    $env:OPENAI_API_KEY = "your-api-key-here"
    
  Or add to your machine environment permanently:
    setx OPENAI_API_KEY "your-api-key-here"
    
  Linux/Mac:
    export OPENAI_API_KEY="your-api-key-here"

STEP 2: Update Program.cs to use OpenAI planner

  Replace this:
    var planner = new RuleBasedPlanner(registry);
    
  With this:
    var planner = await OpenAIPlannerFactory.CreateAsync(registry);

STEP 3: Build and Run

    dotnet build
    dotnet run --project AgenticAI/AgenticAI.csproj


📝 FULL EXAMPLE - REPLACE PATTERN 1
═════════════════════════════════════════════════════════════════════════════════

Before (Rule-based):
─────────────────────────────────────────────────────────────────────────────

    var registry = new ToolRegistry();
    registry.Register(new WebSearchTool());
    registry.Register(new HttpFetchTool());
    registry.Register(new FileTool());
    registry.Register(new CalculatorTool());

    var memory = new InMemoryStore();
    var planner = new RuleBasedPlanner(registry);  // ← Rule-based
    var orchestrator = new AgentOrchestrator(planner, registry, memory);


After (LLM-based):
─────────────────────────────────────────────────────────────────────────────

    var registry = new ToolRegistry();
    registry.Register(new WebSearchTool());
    registry.Register(new HttpFetchTool());
    registry.Register(new FileTool());
    registry.Register(new CalculatorTool());

    var memory = new InMemoryStore();
    
    // Use LLM-based planner instead
    var planner = await OpenAIPlannerFactory.CreateAsync(registry);
    
    var orchestrator = new AgentOrchestrator(planner, registry, memory);


🔑 OPENAI API KEY
═════════════════════════════════════════════════════════════════════════════════

WHERE TO GET YOUR API KEY:

1. Go to: https://platform.openai.com/api-keys
2. Sign in with your OpenAI account
3. Click "Create new secret key"
4. Copy and save it (you won't see it again!)
5. Set environment variable (see Quick Start Step 1)

COST:
  • GPT-4 Turbo: ~$0.01-0.03 per task (depending on complexity)
  • GPT-3.5: ~$0.001-0.002 per task (much cheaper)
  • Default model: gpt-4-turbo

MODELS AVAILABLE:
  • gpt-4-turbo ............ Most capable (recommended for complex planning)
  • gpt-4 .................. Classic GPT-4
  • gpt-3.5-turbo ......... Fast and cheap
  • gpt-4o ................ Latest multi-modal model (if available)

BUDGET TIPS:
  • Test with gpt-3.5-turbo first (much cheaper)
  • Set usage limits at: https://platform.openai.com/account/billing/limits
  • Monitor usage at: https://platform.openai.com/account/usage/overview


🌐 AZURE OPENAI SETUP
═════════════════════════════════════════════════════════════════════════════════

If you prefer Azure OpenAI (enterprise):

STEP 1: Set environment variables

  Windows:
    $env:AZURE_OPENAI_ENDPOINT = "https://your-resource.openai.azure.com/"
    $env:AZURE_OPENAI_API_KEY = "your-api-key"
    $env:AZURE_OPENAI_DEPLOYMENT_NAME = "gpt-4-turbo"

  Linux/Mac:
    export AZURE_OPENAI_ENDPOINT="https://..."
    export AZURE_OPENAI_API_KEY="..."
    export AZURE_OPENAI_DEPLOYMENT_NAME="..."

STEP 2: Use Azure factory

  var planner = AzureOpenAIPlannerFactory.Create(registry);


✅ CONFIGURATION OPTIONS
═════════════════════════════════════════════════════════════════════════════════

OpenAI Factory Supports:

Environment Variables:
  • OPENAI_API_KEY ........... (Required) Your OpenAI API key
  • OPENAI_MODEL ............ (Optional) Model name (default: gpt-4-turbo)
  • OPENAI_ENDPOINT ......... (Optional) Custom endpoint (for Azure, etc.)

Models:
  • gpt-4-turbo (default)
  • gpt-4
  • gpt-3.5-turbo
  • gpt-4o

Fallback:
  • If LLM call fails, automatically falls back to RuleBasedPlanner
  • You'll see a warning in console
  • Agent continues working


🧪 TESTING THE LLM PLANNER
═════════════════════════════════════════════════════════════════════════════════

BASIC TEST:

  1. Set your API key:
     $env:OPENAI_API_KEY = "your-key"
  
  2. Update Program.cs (Pattern 1):
     var planner = await OpenAIPlannerFactory.CreateAsync(registry);
  
  3. Run and watch the console:
     dotnet run --project AgenticAI/AgenticAI.csproj
  
  4. You'll see:
     [PlanCreated] Starting goal: Research .NET 9...
     [ToolCalled] → web_search(...)
     [ToolCalled] → http_fetch(...)
     ...


WHAT THE LLM PLANNER DOES:

1. You give it a goal: "Research .NET 9 and summarize"
2. It analyzes available tools:
   - web_search
   - http_fetch
   - file
   - calculator
   - etc.
3. It creates a structured plan:
   Step 1: Search for ".NET 9 features"
   Step 2: Fetch detailed info from documentation
   Step 3: Save summary to file
4. Returns AgentPlan with ordered steps and dependencies
5. Orchestrator executes the plan


📊 COMPARING PLANNERS
═════════════════════════════════════════════════════════════════════════════════

Feature                    | Rule-Based | OpenAI | Azure OpenAI
─────────────────────────────────────────────────────────────
Cost                       | Free       | $0.01  | Depends on tier
Speed                      | ~1ms       | ~2-5s  | ~2-5s
Reliability                | 100%       | 99.9%  | 99.95%
Flexibility                | Limited    | Excellent | Excellent
Requires API Key           | No         | Yes    | Yes
Works Offline              | Yes        | No     | No
Supports Custom Logic      | No         | Yes    | Yes
Best for Production        | Simple     | Complex| Enterprise
Deterministic              | Yes        | No     | No
Handles Unseen Goals       | No         | Yes    | Yes


💡 USAGE EXAMPLES
═════════════════════════════════════════════════════════════════════════════════

EXAMPLE 1: Research a topic (LLM decides steps)
─────────────────────────────────────────────────────────────────────────────

    var planner = await OpenAIPlannerFactory.CreateAsync(registry);
    var orchestrator = new AgentOrchestrator(planner, registry, memory);
    
    var result = await orchestrator.RunAsync(
        "Research quantum computing advancements in 2024 and create a summary"
    );
    
    // LLM decides:
    // 1. Search for "quantum computing 2024"
    // 2. Fetch detailed articles
    // 3. Analyze key findings
    // 4. Write summary


EXAMPLE 2: Complex calculation with research
─────────────────────────────────────────────────────────────────────────────

    var result = await orchestrator.RunAsync(
        "Calculate the ROI if I invest $10,000 at 7% annually for 5 years, " +
        "and then research what the average stock market return is"
    );
    
    // LLM decides:
    // 1. Calculate investment growth
    // 2. Search for average stock returns
    // 3. Compare results
    // 4. Save analysis


EXAMPLE 3: Dynamic goal (impossible for rule-based planner)
─────────────────────────────────────────────────────────────────────────────

    var result = await orchestrator.RunAsync(
        "Check the current weather in London, compare with historical data, " +
        "and predict if umbrella would be useful next week"
    );
    
    // LLM creates a multi-step plan for a goal it's never seen before


⚙️ ADVANCED CONFIGURATION
═════════════════════════════════════════════════════════════════════════════════

CUSTOMIZE THE PROMPT:

The LLM planner uses a specific prompt template. You can create your own:

    public class CustomOpenAIPlanner : OpenAIPlanner
    {
        protected override string BuildPrompt(string goal, IReadOnlyDictionary<string, object> context)
        {
            // Your custom prompt here
            return base.BuildPrompt(goal, context);
        }
    }

CHANGE MODEL MID-EXECUTION:

    var planner1 = await OpenAIPlannerFactory.CreateAsync(registry);
    // Uses OPENAI_MODEL env var
    
    // Or override:
    var planner2 = new OpenAIPlanner(registry, apiKey, "gpt-3.5-turbo");
    // Uses cheaper model


IMPLEMENT A DIFFERENT LLM PROVIDER:

Create your own planner by implementing IPlanner:

    public class MyChatGPTPlanner : IPlanner
    {
        public async Task<AgentPlan> CreatePlanAsync(
            string goal,
            IReadOnlyDictionary<string, object> context,
            CancellationToken ct = default)
        {
            // Your implementation here
            // Call your LLM API
            // Parse response into AgentPlan
            // Return plan
        }
    }


🔧 TROUBLESHOOTING
═════════════════════════════════════════════════════════════════════════════════

ERROR: "OPENAI_API_KEY environment variable not set"
───────────────────────────────────────────────────────────────────────────────

  Solution:
    1. Set API key: $env:OPENAI_API_KEY = "sk-..."
    2. Or use CreateMockAsync for testing without key:
       var planner = OpenAIPlannerFactory.CreateMockAsync(registry);


ERROR: "Unauthorized - invalid API key"
───────────────────────────────────────────────────────────────────────────────

  Solution:
    1. Check API key is correct (go to https://platform.openai.com/api-keys)
    2. Verify it hasn't been revoked
    3. Check if it has access to the model you're using


ERROR: "Rate limit exceeded"
───────────────────────────────────────────────────────────────────────────────

  Solution:
    1. Wait a moment and try again
    2. Set billing limits at: https://platform.openai.com/account/billing/limits
    3. Use cheaper model: gpt-3.5-turbo


ERROR: "LLM planning failed. Falling back to rule-based planner"
───────────────────────────────────────────────────────────────────────────────

  Normal behavior! The planner:
    1. Tries LLM planning
    2. If it fails, falls back to RuleBasedPlanner
    3. Shows warning in console
    4. Agent continues working
  
  To debug:
    1. Check internet connection
    2. Verify API key
    3. Check OpenAI status: https://status.openai.com/


ERROR: Response timeout
───────────────────────────────────────────────────────────────────────────────

  Solution:
    1. Check internet connection
    2. Use CancellationToken with timeout:
       var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
       var result = await orchestrator.RunAsync(goal, cts.Token);


🎓 BEST PRACTICES
═════════════════════════════════════════════════════════════════════════════════

1. FALLBACK STRATEGY
   ✓ LLM planner falls back to rule-based automatically
   ✓ No manual error handling needed
   ✓ Agent continues even if LLM fails

2. COST MANAGEMENT
   ✓ Start with gpt-3.5-turbo (much cheaper)
   ✓ Use gpt-4-turbo only for complex tasks
   ✓ Monitor usage regularly

3. PERFORMANCE
   ✓ LLM planner takes 2-5 seconds for planning
   ✓ Tool execution is separate (can be slow)
   ✓ Cache plans if same goal is used repeatedly

4. RELIABILITY
   ✓ Always provide fallback planner (built-in)
   ✓ Test with both planners in CI/CD
   ✓ Monitor agent success rates

5. SECURITY
   ✓ Never hardcode API keys
   ✓ Use environment variables only
   ✓ Rotate keys regularly
   ✓ Use Azure OpenAI for enterprise compliance


📚 NEXT STEPS
═════════════════════════════════════════════════════════════════════════════════

1. Set OpenAI API key (see Quick Start)
2. Update Program.cs to use OpenAIPlannerFactory
3. Run and test
4. Monitor costs
5. Optimize prompts based on results
6. Consider Azure OpenAI for production


📖 REFERENCES
═════════════════════════════════════════════════════════════════════════════════

OpenAI API Documentation:
  https://platform.openai.com/docs/api-reference

Pricing:
  https://openai.com/pricing

Azure OpenAI Documentation:
  https://learn.microsoft.com/en-us/azure/cognitive-services/openai/

Your API Keys:
  https://platform.openai.com/api-keys

Billing & Usage:
  https://platform.openai.com/account/billing/overview


═════════════════════════════════════════════════════════════════════════════════

                    Ready to Use LLM-Based Planning! 🚀

             Set your API key and start planning with AI!

═════════════════════════════════════════════════════════════════════════════════
