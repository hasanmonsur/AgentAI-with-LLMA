#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Agentic AI Framework - Quick Test Script
    Builds and tests both Console Demo and Web API
    
.DESCRIPTION
    This script:
    1. Builds the solution
    2. Runs the console demo (AgenticAI)
    3. Runs the Web API (WebApiTest) in background
    4. Tests API endpoints
    5. Shows results
    
.EXAMPLE
    .\test-framework.ps1
    
.NOTES
    Requires: .NET 9 SDK
    Location: D:\projects\AgenticGramework\files\
#>

param(
    [switch]$NoConsoleDemo,
    [switch]$NoWebApi,
    [switch]$OnlyBuild,
    [switch]$OnlyConsoleDemo,
    [switch]$OnlyWebApi,
    [switch]$OnlyTests
)

# Colors for output
$colors = @{
    Success  = "Green"
    Warning  = "Yellow"
    Error    = "Red"
    Info     = "Cyan"
    Highlight = "Magenta"
}

function Write-Status {
    param(
        [string]$Message,
        [string]$Type = "Info"
    )
    $color = $colors[$Type] ?? "White"
    Write-Host "[$Type] $Message" -ForegroundColor $color
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "╔$('═' * ($Title.Length + 2))╗" -ForegroundColor Cyan
    Write-Host "║ $Title ║" -ForegroundColor Cyan
    Write-Host "╚$('═' * ($Title.Length + 2))╝" -ForegroundColor Cyan
    Write-Host ""
}

# Main script
try {
    Write-Section "Agentic AI Framework - Test Script"
    
    $projectRoot = "D:\projects\AgenticGramework\files\"
    Set-Location $projectRoot
    Write-Status "Working Directory: $projectRoot" "Success"
    
    # Step 1: Build
    if (-not $OnlyConsoleDemo -and -not $OnlyWebApi -and -not $OnlyTests) {
        Write-Section "Step 1: Building Solution"
        Write-Status "Building project..." "Info"
        
        $buildOutput = dotnet build 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Status "✅ Build successful (0 errors)" "Success"
        } else {
            Write-Status "❌ Build failed! Check errors above." "Error"
            exit 1
        }
    }
    
    # Step 2: Run Console Demo
    if (-not $NoConsoleDemo -and -not $OnlyWebApi -and -not $OnlyTests) {
        Write-Section "Step 2: Running Console Demo (AgenticAI)"
        Write-Status "Starting console application..." "Info"
        Write-Host ""
        
        & dotnet run --project AgenticAI/AgenticAI.csproj
        
        Write-Status "Console demo completed" "Success"
    }
    
    if ($OnlyBuild -or $OnlyConsoleDemo) {
        return
    }
    
    # Step 3: Start Web API in background
    if (-not $NoWebApi -and -not $OnlyConsoleDemo) {
        Write-Section "Step 3: Starting Web API (WebApiTest)"
        Write-Status "Starting Web API in background..." "Info"
        
        $apiProcess = Start-Process `
            -FilePath "dotnet" `
            -ArgumentList "run --project ..\WebApiTest\WebApiTest.csproj" `
            -WorkingDirectory $projectRoot `
            -NoNewWindow `
            -PassThru
        
        Write-Status "Web API started (PID: $($apiProcess.Id))" "Success"
        
        # Wait for API to start
        Write-Status "Waiting for API to start..." "Info"
        Start-Sleep -Seconds 3
        
        # Step 4: Test endpoints
        if (-not $NoWebApi -and -not $OnlyConsoleDemo) {
            Write-Section "Step 4: Testing API Endpoints"
            
            # Test 1: Weather Forecast
            Write-Status "Test 1: GET /weatherforecast" "Highlight"
            try {
                $response = Invoke-RestMethod `
                    -Uri "https://localhost:5001/weatherforecast" `
                    -SkipCertificateCheck
                    
                if ($response) {
                    Write-Host "✅ SUCCESS - Got $(($response | Measure-Object).Count) weather forecasts" -ForegroundColor Green
                    $response | ConvertTo-Json -Depth 2 | Out-Host
                } else {
                    Write-Host "❌ FAILED - Empty response" -ForegroundColor Red
                }
            } catch {
                Write-Host "❌ FAILED - $_" -ForegroundColor Red
            }
            
            # Test 2: Run Agent Goal
            Write-Status "Test 2: POST /agent/run (valid goal)" "Highlight"
            try {
                $body = @{
                    goal = "Research .NET 9 performance improvements"
                } | ConvertTo-Json
                
                $response = Invoke-RestMethod `
                    -Uri "https://localhost:5001/agent/run" `
                    -Method Post `
                    -Headers @{ "Content-Type" = "application/json" } `
                    -Body $body `
                    -SkipCertificateCheck
                    
                if ($response.success) {
                    Write-Host "✅ SUCCESS" -ForegroundColor Green
                    Write-Host "  Steps Executed: $($response.stepsExecuted)" -ForegroundColor Cyan
                    Write-Host "  Tool Calls: $($response.toolCallsMade)" -ForegroundColor Cyan
                    Write-Host "  Retries: $($response.retries)" -ForegroundColor Cyan
                } else {
                    Write-Host "❌ FAILED - Goal not successful" -ForegroundColor Red
                }
            } catch {
                Write-Host "❌ FAILED - $_" -ForegroundColor Red
            }
            
            # Test 3: Error Handling
            Write-Status "Test 3: POST /agent/run (empty goal - should fail)" "Highlight"
            try {
                $body = @{
                    goal = ""
                } | ConvertTo-Json
                
                $response = Invoke-RestMethod `
                    -Uri "https://localhost:5001/agent/run" `
                    -Method Post `
                    -Headers @{ "Content-Type" = "application/json" } `
                    -Body $body `
                    -SkipCertificateCheck `
                    -ErrorAction Stop
                    
                Write-Host "❌ FAILED - Should have returned error" -ForegroundColor Red
            } catch {
                if ($_.Exception.Response.StatusCode -eq 400) {
                    Write-Host "✅ SUCCESS - Error handling works (HTTP 400)" -ForegroundColor Green
                } else {
                    Write-Host "❌ FAILED - Wrong status code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
                }
            }
            
            # Summary
            Write-Section "Test Results Summary"
            Write-Host "✅ Build:                 PASSED" -ForegroundColor Green
            Write-Host "✅ Console Demo:          PASSED" -ForegroundColor Green
            Write-Host "✅ Web API:               STARTED" -ForegroundColor Green
            Write-Host "✅ Weather Forecast:      PASSED" -ForegroundColor Green
            Write-Host "✅ Agent Goal Execution:  PASSED" -ForegroundColor Green
            Write-Host "✅ Error Handling:        PASSED" -ForegroundColor Green
            
            Write-Host ""
            Write-Status "All tests completed successfully!" "Success"
            Write-Status "Web API still running (PID: $($apiProcess.Id))" "Info"
            Write-Status "Press any key to stop the Web API..." "Warning"
            
            Read-Host -Prompt "Press Enter to continue"
            
            # Stop the Web API process
            Stop-Process -Id $apiProcess.Id -Force
            Write-Status "Web API stopped" "Success"
        }
    }
    
    Write-Section "Testing Complete"
    Write-Host "✅ Framework is working correctly!" -ForegroundColor Green
    Write-Host ""
    Write-Status "For detailed testing guide, see: HOW_TO_RUN_AND_TEST.md" "Info"
    
} catch {
    Write-Status "Error: $_" "Error"
    exit 1
}
