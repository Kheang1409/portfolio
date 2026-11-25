# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first to leverage Docker cache for restore
COPY KaiAssistant.sln ./
COPY KaiAssistant.API/*.csproj ./KaiAssistant.API/
COPY KaiAssistant.Application/*.csproj ./KaiAssistant.Application/
COPY KaiAssistant.Domain/*.csproj ./KaiAssistant.Domain/
COPY KaiAssistant.Infrastructure/*.csproj ./KaiAssistant.Infrastructure/
COPY KaiAssistant.Tests/*.csproj ./KaiAssistant.Tests/
COPY KaiAssistant.RagEvaluation/*.csproj ./KaiAssistant.RagEvaluation/

# Restore NuGet packages
RUN dotnet restore ./KaiAssistant.sln

# Copy the remaining source code
COPY . .

# Publish the app
RUN dotnet publish ./KaiAssistant.API/KaiAssistant.API.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system appgroup && useradd --system --gid appgroup appuser

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app from build stage
COPY --from=build /app/publish .

EXPOSE 8080

USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Start the app
ENTRYPOINT ["dotnet", "KaiAssistant.API.dll"]
