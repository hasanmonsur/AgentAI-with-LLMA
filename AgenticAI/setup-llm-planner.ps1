#!/usr/bin/env pwsh
<#
.SYNOPSIS
    LLM Planner Quick Setup & Test Script
    
.DESCRIPTION
    Sets up and tests the LLM-based planner with OpenAI
    
.PARAMETER ApiKey
    Your OpenAI API key (optional, will prompt if not provided)
    
.PARAMETER Model
    OpenAI model to use (default: gpt-4-turbo)
    
.EXAMPLE
    .\setup-llm-planner.ps1 -ApiKey "sk-..."
    .\setup-llm-planner.ps1  # Will prompt for key
    
.NOTES
    Requires .NET 9 SDK
#>

param(
    [string]$ApiKey = "",
    [string]$Model = "gpt-4-turbo",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

# Colors
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Error { Write-Host $args -ForegroundColor Red }

Write-Host ""
Write-Info "╔════════════════════════════════════════════════════════════════╗"
Write-Info "║         LLM Planner Setup & Test - Quick Start               ║"
Write-Info "╚════════════════════════════════════════════════════════════════╝"
Write-Host ""

# Step 1: Verify .NET
Write-Info "📋 Step 1: Verifying .NET 9"
$dotnetVersion = dotnet --version
Write-Success "✓ .NET version: $dotnetVersion"

# Step 2: Get API key if not provided
if ([string]::IsNullOrEmpty($ApiKey)) {
    Write-Info ""
    Write-Info "🔑 Step 2: OpenAI API Key"
    Write-Warning "Get your key from: https://platform.openai.com/api-keys"
    $ApiKey = Read-Host "Enter your OpenAI API key (sk-...)"
    
    if ([string]::IsNullOrEmpty($ApiKey)) {
        Write-Warning "No API key provided. Using rule-based planner fallback."
        $ApiKey = ""
    }
}

# Step 3: Set environment variable
Write-Info ""
Write-Info "🌍 Step 3: Setting Environment Variable"
$env:OPENAI_API_KEY = $ApiKey
$env:OPENAI_MODEL = $Model
Write-Success "✓ OPENAI_API_KEY set"
Write-Success "✓ OPENAI_MODEL = $Model"

# Step 4: Build
if (-not $SkipBuild) {
    Write-Info ""
    Write-Info "🔨 Step 4: Building Project"
    
    $buildOutput = dotnet build 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "✓ Build successful"
    } else {
        Write-Error "✗ Build failed"
        Write-Error $buildOutput
        exit 1
    }
} else {
    Write-Info ""
    Write-Info "⏭️  Step 4: Skipping build"
}

# Step 5: Summary
Write-Info ""
Write-Info "════════════════════════════════════════════════════════════════"
Write-Success "✅ Setup Complete!"
Write-Info "════════════════════════════════════════════════════════════════"

Write-Host ""
Write-Info "📝 Next Steps:"
Write-Host ""
Write-Host "1. Update your Program.cs:"
Write-Host "   Replace:"
Write-Host "     var planner = new RuleBasedPlanner(registry);"
Write-Host "   With:"
Write-Host "     var planner = await OpenAIPlannerFactory.CreateAsync(registry);"
Write-Host ""
Write-Host "2. Run your application:"
Write-Host "   dotnet run"
Write-Host ""
Write-Host "3. Verify it works:"
Write-Host "   Look for: [PlanCreated] messages in console"
Write-Host ""

if ([string]::IsNullOrEmpty($ApiKey)) {
    Write-Warning "⚠️  No API key provided"
    Write-Warning "   The planner will fall back to RuleBasedPlanner"
    Write-Warning "   Set key to enable LLM planning: \$env:OPENAI_API_KEY = 'sk-...'"
} else {
    Write-Success "✅ API Key configured - LLM planning enabled!"
}

Write-Host ""
Write-Info "📚 Documentation:"
Write-Host "   • LLM_PLANNER_QUICK_REFERENCE.md .......... 5-min setup"
Write-Host "   • LLM_PLANNER_SETUP.md ................... Full guide"
Write-Host "   • LLM_PLANNER_IMPLEMENTATION_GUIDE.md .... Examples"
Write-Host ""

Write-Info "🚀 Ready to use LLM-based planning!"
Write-Host ""
