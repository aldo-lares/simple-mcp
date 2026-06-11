using SimpleMcp.Server;

namespace SimpleMcp.Tests;

public class McpToolsTests
{
    private readonly McpTools _tools = new();

    [Fact]
    public void Echo_ReturnsInputMessage()
    {
        var result = _tools.Echo("hello world");
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void Echo_ReturnsEmptyString_WhenGivenEmpty()
    {
        var result = _tools.Echo(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(-5, 5, 0)]
    [InlineData(0.5, 0.5, 1.0)]
    [InlineData(double.MaxValue / 2, double.MaxValue / 2, double.MaxValue)]
    public void Sum_ReturnsSumOfTwoNumbers(double a, double b, double expected)
    {
        var result = _tools.Sum(a, b);
        Assert.Equal(expected, result, precision: 10);
    }

    [Fact]
    public void Time_ReturnsIso8601UtcString()
    {
        var before = DateTime.UtcNow;
        var result = _tools.Time();
        var after = DateTime.UtcNow;

        var parsed = DateTime.Parse(result, null, System.Globalization.DateTimeStyles.RoundtripKind);
        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
        Assert.True(parsed >= before && parsed <= after);
    }
}
