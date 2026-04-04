using System.Text.Json.Nodes;
using AgenticAI;

namespace WebApiTest;

/// <summary>
/// Slack integration tool for sending messages and notifications.
/// In production, integrate with Slack API (https://api.slack.com).
/// </summary>
public sealed class SlackTool : AgentToolBase
{
    private readonly string _token;

    public SlackTool(string slackToken)
    {
        _token = slackToken ?? throw new ArgumentNullException(nameof(slackToken));
    }

    public override string Name => "slack";
    public override string Description => "Send messages to Slack. Params: { channel: string, message: string }";

    protected override async Task<ToolResult> RunAsync(JsonObject p, CancellationToken ct)
    {
        try
        {
            var channel = RequireParam(p, "channel");
            var message = RequireParam(p, "message");

            // Validate inputs
            if (string.IsNullOrWhiteSpace(_token))
                return ToolResult.Fail("Slack token is not configured.");

            if (!channel.StartsWith("#") && !channel.StartsWith("@"))
                return ToolResult.Fail("Channel must start with # (for channels) or @ (for users)");

            // Simulate Slack API call
            await Task.Delay(100, ct);

            // In production, call: https://slack.com/api/chat.postMessage
            var result = $"✓ Message sent to {channel}: {message.Substring(0, Math.Min(50, message.Length))}...";

            return ToolResult.Ok(result);
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"Slack tool error: {ex.Message}");
        }
    }
}
