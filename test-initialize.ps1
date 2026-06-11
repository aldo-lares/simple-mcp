$body = @{
    jsonrpc = "2.0"
    id = 1
    method = "initialize"
    params = @{
        protocolVersion = "2025-03-26"
        capabilities = @{}
        clientInfo = @{
            name = "test-client"
            version = "1.0"
        }
    }
} | ConvertTo-Json -Depth 10

Write-Host "Sending request to http://localhost:5057/mcp"
Write-Host "Body: $body"
Write-Host ""

try {
    $headers = @{
        "Content-Type" = "application/json"
        "Accept" = "application/json, text/event-stream"
    }
    
    $response = Invoke-WebRequest -Uri "http://localhost:5057/mcp" `
      -Method POST `
      -Headers $headers `
      -Body $body `
      -UseBasicParsing
    
    Write-Host "Status: $($response.StatusCode)"
    Write-Host "Session ID Header: $($response.Headers['MCP-Session-Id'])"
    Write-Host ""
    Write-Host "Response Content:"
    Write-Host $response.Content
    
    # Try to parse SSE format
    $lines = $response.Content -split "`n"
    Write-Host ""
    Write-Host "Parsed lines:"
    foreach ($line in $lines) {
        if ($line.Trim().StartsWith("data: {")) {
            $json = $line.Trim() -replace "^data: ", ""
            Write-Host "JSON line found: $json"
            $obj = $json | ConvertFrom-Json
            Write-Host "sessionId in result: $($obj.result.sessionId)"
        }
    }
} catch {
    Write-Host "Error: $_"
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
}
