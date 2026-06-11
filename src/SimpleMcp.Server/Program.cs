using SimpleMcp.Server;
using System.Text;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHttpContextAccessor()
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<McpTools>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/mcp") || !HttpMethods.IsPost(context.Request.Method))
    {
        await next();
        return;
    }

    var sessionId = context.Request.Headers["MCP-Session-Id"].ToString();
    if (string.IsNullOrWhiteSpace(sessionId))
    {
        sessionId = Guid.NewGuid().ToString("N");
    }

    context.Items["MCP-Session-Id"] = sessionId;
    context.Response.Headers["MCP-Session-Id"] = sessionId;

    var isInitialize = await IsInitializeRequestAsync(context.Request);
    if (!isInitialize)
    {
        await next();
        return;
    }

    var originalBody = context.Response.Body;
    using var capturedBody = new MemoryStream();
    context.Response.Body = capturedBody;

    await next();

    capturedBody.Seek(0, SeekOrigin.Begin);
    using var reader = new StreamReader(capturedBody);
    var payload = await reader.ReadToEndAsync();
    var rewrittenPayload = InjectSessionIdIntoInitializePayload(payload, sessionId);

    // Clear the original response and write new content
    context.Response.Body = originalBody;
    
    // Write the modified payload
    var bytes = Encoding.UTF8.GetBytes(rewrittenPayload);
    await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
});

static async Task<bool> IsInitializeRequestAsync(HttpRequest request)
{
    if (!HttpMethods.IsPost(request.Method))
    {
        return false;
    }

    request.EnableBuffering();

    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
    var body = await reader.ReadToEndAsync();
    request.Body.Position = 0;

    if (string.IsNullOrWhiteSpace(body))
    {
        return false;
    }

    try
    {
        var root = JsonNode.Parse(body) as JsonObject;
        var method = root?["method"]?.GetValue<string>();
        return string.Equals(method, "initialize", StringComparison.Ordinal);
    }
    catch
    {
        return false;
    }
}

static string InjectSessionIdIntoInitializePayload(string payload, string sessionId)
{
    if (string.IsNullOrWhiteSpace(payload))
    {
        return payload;
    }

    var lines = payload.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    var newline = payload.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

    for (var i = 0; i < lines.Length; i++)
    {
        var line = lines[i].Trim();
        if (!line.StartsWith("data: {", StringComparison.Ordinal))
        {
            continue;
        }

        try
        {
            var jsonText = line.Substring(6);
            var envelope = JsonNode.Parse(jsonText) as JsonObject;
            
            if (envelope == null)
            {
                continue;
            }

            var result = envelope["result"] as JsonObject;
            if (result == null || !result.ContainsKey("serverInfo"))
            {
                continue;
            }

            result["sessionId"] = sessionId;
            lines[i] = "data: " + envelope.ToJsonString();
            break;
        }
        catch
        {
            continue;
        }
    }

    return string.Join(newline, lines);
}

app.MapMcp("/mcp");

app.MapGet("/", () => Results.Ok(new
{
    Name = "SimpleMcp.Server",
    McpEndpoint = "/mcp"
}));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/ready", () => Results.Ok(new { status = "ready" }));

app.Run();
