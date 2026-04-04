╔═══════════════════════════════════════════════════════════════════════════════╗
║                                                                               ║
║                  LLM PLANNER - QUICK REFERENCE CARD                          ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝


🎯 ONE-LINE SUMMARY
═════════════════════════════════════════════════════════════════════════════════

Replace `RuleBasedPlanner` with `OpenAIPlannerFactory.CreateAsync()` to use
GPT-4 for dynamic task decomposition instead of hardcoded keyword matching.


⚡ QUICK SETUP (3 Steps)
═════════════════════════════════════════════════════════════════════════════════

1. SET API KEY
   $env:OPENAI_API_KEY = "sk-..."

2. UPDATE CODE
   Old: var planner = new RuleBasedPlanner(registry);
   New: var planner = await OpenAIPlannerFactory.CreateAsync(registry);

3. RUN
   dotnet build
   dotnet run


📊 COMPARISON TABLE
═════════════════════════════════════════════════════════════════════════════════

                          RuleBasedPlanner    OpenAIPlannerFactory
─────────────────────────────────────────────────────────────────────────────
Setup Complexity          Simple              Medium (requires API key)
Speed                     ~1ms                ~2-5 seconds
Cost                      Free                $0.01-0.03 per call
Flexibility               Hardcoded rules     Dynamic AI reasoning
Handles New Goals         ❌ No               ✅ Yes
Offline Support           ✅ Yes              ❌ No (requires API)
Reliability               ✅ 100%             ✅ 99.9%
Best For                  Dev/Testing         Production/Complex tasks
Requires Network          ❌ No               ✅ Yes


🔑 API KEYS
═════════════════════════════════════════════════════════════════════════════════

OpenAI:
  Get: https://platform.openai.com/api-keys
  Set: $env:OPENAI_API_KEY = "your-key"
  Cost: Pay-as-you-go ($0.01-0.03 per plan)

Azure OpenAI:
  Get: Azure Portal → Create OpenAI resource
  Set: Three env vars (see LLM_PLANNER_SETUP.md)
  Cost: Depends on tier


💻 CODE EXAMPLES
═════════════════════════════════════════════════════════════════════════════════

MINIMAL EXAMPLE:
───────────────────────────────────────────────────────────────────────────────

    var registry = new ToolRegistry();
    registry.Register(new WebSearchTool());
    
    // This is the only change needed:
    var planner = await OpenAIPlannerFactory.CreateAsync(registry);
    
    var orchestrator = new AgentOrchestrator(planner, registry, new InMemoryStore());
    var result = await orchestrator.RunAsync("Research AI trends");


WITH ERROR HANDLING:
───────────────────────────────────────────────────────────────────────────────

    IPlanner planner;
    try
    {
        planner = await OpenAIPlannerFactory.CreateAsync(registry);
    }
    catch (InvalidOperationException)
    {
        // Fallback if no API key
        planner = new RuleBasedPlanner(registry);
    }
    
    var orchestrator = new AgentOrchestrator(planner, registry, memory);


WITH TIMEOUT:
───────────────────────────────────────────────────────────────────────────────

    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    var result = await orchestrator.RunAsync("Research topic", cts.Token);


USING AZURE:
───────────────────────────────────────────────────────────────────────────────

    // Set env vars first:
    // AZURE_OPENAI_ENDPOINT = "https://..."
    // AZURE_OPENAI_API_KEY = "..."
    // AZURE_OPENAI_DEPLOYMENT_NAME = "..."
    
    var planner = AzureOpenAIPlannerFactory.Create(registry);


🎓 HOW IT WORKS
═════════════════════════════════════════════════════════════════════════════════

INPUT:  Goal = "Research .NET 9 performance"

PROCESSING:
  1. Factory creates OpenAIPlanner instance
  2. Planner builds prompt with available tools
  3. Sends to OpenAI API (GPT-4 by default)
  4. API returns JSON with step-by-step plan
  5. Planner parses JSON into AgentStep objects
  6. Returns AgentPlan to orchestrator

OUTPUT: AgentPlan with optimized steps and dependencies

EXAMPLE RESPONSE:
  {
    "goal": "Research .NET 9 performance",
    "steps": [
      {
        "id": "step1",
        "description": "Search for .NET 9 performance info",
        "toolName": "web_search",
        "parameters": {"query": "..."},
        "dependsOn": []
      },
      {
        "id": "step2",
        "description": "Fetch detailed article",
        "toolName": "http_fetch",
        "parameters": {"url": "..."},
        "dependsOn": ["step1"]
      }
    ]
  }


⚙️ CONFIGURATION
═════════════════════════════════════════════════════════════════════════════════

Environment Variables (OpenAI):

  OPENAI_API_KEY (Required)
    Your OpenAI API key
    Get from: https://platform.openai.com/api-keys

  OPENAI_MODEL (Optional)
    Default: "gpt-4-turbo"
    Options: "gpt-4", "gpt-3.5-turbo", "gpt-4o"

  OPENAI_ENDPOINT (Optional)
    Custom endpoint URL (for Azure, etc.)


Set in PowerShell:

  # Temporary (current session only)
  $env:OPENAI_API_KEY = "sk-..."
  
  # Permanent (all future sessions)
  setx OPENAI_API_KEY "sk-..."


✅ TESTING CHECKLIST
═════════════════════════════════════════════════════════════════════════════════

Before using in production:

  ☐ OpenAI API key obtained and set
  ☐ Code updated to use OpenAIPlannerFactory
  ☐ Tested with simple goal (e.g., "calculate 2+2")
  ☐ Tested with complex goal (e.g., "research and summarize")
  ☐ Error handling in place (fallback to RuleBasedPlanner)
  ☐ Performance acceptable (2-5 second plan creation)
  ☐ Costs within budget
  ☐ Reviewed generated plans for correctness


🚨 TROUBLESHOOTING
═════════════════════════════════════════════════════════════════════════════════

"OPENAI_API_KEY not set"
  → Run: $env:OPENAI_API_KEY = "your-key"
  → Or: setx OPENAI_API_KEY "your-key"

"Invalid API key"
  → Check: https://platform.openai.com/api-keys
  → Verify key isn't revoked
  → Try creating new key

"Rate limit exceeded"
  → Wait a moment and retry
  → Check: https://platform.openai.com/account/billing/limits
  → Set usage limit to prevent overages

"Falling back to rule-based planner"
  → Normal! LLM call failed
  → Check: Internet connection, API key, quota
  → Agent continues working with RuleBasedPlanner


📈 COST ESTIMATE
═════════════════════════════════════════════════════════════════════════════════

Using GPT-4-Turbo (default):
  • Simple goal: $0.001-0.005 per plan
  • Complex goal: $0.01-0.03 per plan
  • 100 goals/day: $0.10-3.00 per day

Using GPT-3.5-Turbo (cheaper):
  • Simple goal: $0.0001-0.0005 per plan
  • Complex goal: $0.001-0.003 per plan
  • 100 goals/day: $0.01-0.30 per day

Set model:
  var planner = new OpenAIPlanner(registry, apiKey, "gpt-3.5-turbo");


🔗 LINKS & RESOURCES
═════════════════════════════════════════════════════════════════════════════════

Documentation Files:
  • LLM_PLANNER_SETUP.md ......... Full setup guide
  • Program.LLMPlanner.Example.cs. Complete example
  • OpenAIPlannerFactory.cs ...... Implementation details

External:
  • OpenAI Platform ............ https://platform.openai.com
  • API Reference ............. https://platform.openai.com/docs/api-reference
  • Pricing ................... https://openai.com/pricing
  • Status Page ............... https://status.openai.com


❓ FAQ
═════════════════════════════════════════════════════════════════════════════════

Q: Do I need to change anything else?
A: No, just update the planner creation line. Fallback is automatic.

Q: What if the API call fails?
A: Automatically falls back to RuleBasedPlanner. You'll see a warning.

Q: How much does it cost?
A: ~$0.01-0.03 per plan with GPT-4, much cheaper with GPT-3.5-turbo.

Q: Can I use a different LLM provider?
A: Yes, implement IPlanner and swap it in.

Q: Is it production-ready?
A: Yes, with built-in error handling and fallback mechanism.

Q: Does it work offline?
A: No, requires internet for API calls.

Q: Can I cache plans?
A: Yes, create a wrapper that caches by goal hash.


═════════════════════════════════════════════════════════════════════════════════

                   Ready to upgrade to LLM-based planning!

                    See LLM_PLANNER_SETUP.md for details.

═════════════════════════════════════════════════════════════════════════════════
