using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SimpleMcp.Server;

[McpServerToolType]
public sealed class McpTools
{
    [McpServerTool, Description("Echoes the provided message back to the caller.")]
    public string Echo([Description("The message to echo")] string message) => message;

    [McpServerTool, Description("Returns the sum of two numbers.")]
    public double Sum(
        [Description("The first number")] double a,
        [Description("The second number")] double b) => a + b;

    [McpServerTool, Description("Returns the current UTC date and time as an ISO 8601 string.")]
    public string Time() => DateTime.UtcNow.ToString("o");
}
