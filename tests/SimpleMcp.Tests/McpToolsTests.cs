using Microsoft.AspNetCore.Http;
using SimpleMcp.Server;

namespace SimpleMcp.Tests;

public class McpToolsTests
{
    private readonly McpTools _tools = new();

    private static IHttpContextAccessor CreateHttpContextAccessor(string? sessionId = "session-123")
    {
        var context = new DefaultHttpContext();
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            context.Request.Headers["MCP-Session-Id"] = sessionId;
        }

        return new HttpContextAccessor { HttpContext = context };
    }

    [Fact]
    public void Echo_ReturnsInputMessage()
    {
        var sessionId = "session-123";
        var result = _tools.Echo("hello world", CreateHttpContextAccessor(sessionId));
        Assert.Equal("hello world", result.Message);
        Assert.Equal(sessionId, result.SessionId);
    }

    [Fact]
    public void Echo_ReturnsEmptyString_WhenGivenEmpty()
    {
        var sessionId = "session-123";
        var result = _tools.Echo(string.Empty, CreateHttpContextAccessor(sessionId));
        Assert.Equal(string.Empty, result.Message);
        Assert.Equal(sessionId, result.SessionId);
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(-5, 5, 0)]
    [InlineData(0.5, 0.5, 1.0)]
    [InlineData(double.MaxValue / 2, double.MaxValue / 2, double.MaxValue)]
    public void Sum_ReturnsSumOfTwoNumbers(double a, double b, double expected)
    {
        var sessionId = "session-123";
        var result = _tools.Sum(a, b, CreateHttpContextAccessor(sessionId));
        Assert.Equal(expected, result.Result, precision: 10);
        Assert.Equal(sessionId, result.SessionId);
    }

    [Fact]
    public void Time_ReturnsIso8601UtcString()
    {
        var sessionId = "session-123";
        var before = DateTime.UtcNow;
        var result = _tools.Time(CreateHttpContextAccessor(sessionId));
        var after = DateTime.UtcNow;

        var parsed = DateTime.Parse(result.IsoUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);
        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
        Assert.True(parsed >= before && parsed <= after);
        Assert.Equal(sessionId, result.SessionId);
    }

    [Fact]
    public void Echo_GeneratesSessionId_WhenSessionHeaderIsMissing()
    {
        var result = _tools.Echo("hello", CreateHttpContextAccessor(sessionId: null));

        Assert.False(string.IsNullOrWhiteSpace(result.SessionId));
        Assert.Equal(32, result.SessionId.Length);
    }
}
