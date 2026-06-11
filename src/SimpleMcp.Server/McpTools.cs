using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SimpleMcp.Server;

[McpServerToolType]
public sealed class McpTools
{
    [McpServerTool, Description("Echoes the provided message back to the caller.")]
    public EchoResponse Echo(
        [Description("The message to echo")] string message,
        IHttpContextAccessor httpContextAccessor)
    {
        var sessionId = GetSessionId(httpContextAccessor);
        return new EchoResponse(sessionId, message);
    }

    [McpServerTool, Description("Returns the sum of two numbers.")]
    public SumResponse Sum(
        [Description("The first number")] double a,
        [Description("The second number")] double b,
        IHttpContextAccessor httpContextAccessor)
    {
        var sessionId = GetSessionId(httpContextAccessor);
        return new SumResponse(sessionId, a + b);
    }

    [McpServerTool, Description("Returns the current UTC date and time as an ISO 8601 string.")]
    public TimeResponse Time(IHttpContextAccessor httpContextAccessor)
    {
        var sessionId = GetSessionId(httpContextAccessor);
        return new TimeResponse(sessionId, DateTime.UtcNow.ToString("o"));
    }

    private static string GetSessionId(IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext;
        var sessionId = context?.Request.Headers["MCP-Session-Id"].ToString();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = context?.Items["MCP-Session-Id"] as string;
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = Guid.NewGuid().ToString("N");

            if (context is not null)
            {
                context.Items["MCP-Session-Id"] = sessionId;
                context.Response.Headers["MCP-Session-Id"] = sessionId;
            }
        }

        return sessionId;
    }

    public sealed record EchoResponse(string SessionId, string Message);
    public sealed record SumResponse(string SessionId, double Result);
    public sealed record TimeResponse(string SessionId, string IsoUtc);
}
