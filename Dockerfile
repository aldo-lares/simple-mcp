# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY ["src/SimpleMcp.Server/SimpleMcp.Server.csproj", "SimpleMcp.Server/"]
RUN dotnet restore "SimpleMcp.Server/SimpleMcp.Server.csproj"

COPY src/SimpleMcp.Server/ SimpleMcp.Server/
RUN dotnet build "SimpleMcp.Server/SimpleMcp.Server.csproj" -c Release -o /app/build

RUN dotnet publish "SimpleMcp.Server/SimpleMcp.Server.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app

COPY --from=build /app/publish .

# Health check for MCP endpoint
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD dotnet SimpleMcp.Server.dll healthcheck || exit 1

EXPOSE 5057

# Run the application
ENTRYPOINT ["dotnet", "SimpleMcp.Server.dll"]
