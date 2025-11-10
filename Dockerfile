# syntax=docker/dockerfile:1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files and restore dependencies (better layer caching)
COPY ["Directory.Build.props", "./"]
COPY ["src/AzuriteUI.Web/AzuriteUI.Web.csproj", "src/AzuriteUI.Web/"]
RUN dotnet restore "src/AzuriteUI.Web/AzuriteUI.Web.csproj"

# Copy source code and build
COPY ["src/AzuriteUI.Web/", "src/AzuriteUI.Web/"]
WORKDIR "/src/src/AzuriteUI.Web"
RUN dotnet build "AzuriteUI.Web.csproj" -c $BUILD_CONFIGURATION --no-restore

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AzuriteUI.Web.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Final runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Create non-root user and group (use different IDs to avoid conflicts)
RUN groupadd -r azuriteui --gid=1001 && \
    useradd -r -g azuriteui --uid=1001 --home-dir=/app --shell=/bin/bash azuriteui && \
    chown -R azuriteui:azuriteui /app

# Copy published app from publish stage
COPY --from=publish --chown=azuriteui:azuriteui /app/publish .

# Create directory for SQLite database with proper permissions
RUN mkdir -p /app/data && chown -R azuriteui:azuriteui /app/data

# Switch to non-root user
USER azuriteui

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/api/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

# Labels for container metadata (OCI standard)
LABEL org.opencontainers.image.title="Azurite UI" \
      org.opencontainers.image.description="Web UI for Azure Storage emulator (Azurite)" \
      org.opencontainers.image.vendor="AzuriteUI" \
      org.opencontainers.image.licenses="MIT" \
      org.opencontainers.image.source="https://github.com/adrianhall/azurite-ui" \
      org.opencontainers.image.documentation="https://github.com/adrianhall/azurite-ui/blob/main/README.md"

# Entry point
ENTRYPOINT ["dotnet", "AzuriteUI.Web.dll"]
