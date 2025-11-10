# Azurite UI - the missing web console for Azurite

A web-based developer console for [Azurite](https://github.com/Azure/Azurite), the Azure Storage emulator. Azurite UI provides an intuitive interface to manage containers and blobs in your local Azurite development environment, eliminating the need for command-line tools or third-party storage explorers.

## Overview

Azurite is a free, open-source emulator that provides a local environment simulating Azure Blob, Queue, and Table storage services for development and testing purposes. Azurite UI complements Azurite by providing a modern web interface for:

- **Container Management**: List, create, delete, and view properties for containers
- **Blob Operations**: Browse, upload, download, delete, preview (images), and view properties for blobs
- **Infinite Scroll**: Seamless browsing of large container lists with offset-based pagination
- **Data Tables**: Paginated blob lists with configurable page sizes and navigation
- **Rich Details**: View metadata, tags, dates, and other properties in slide-out panels
- **Image Preview**: Preview image blobs up to 4MB directly in the browser

Since this is a developer console designed for local development, no authentication is required and denial-of-service protections like rate limiting are not implemented.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (version specified in global.json or project files)
- [Cake](https://cakebuild.net/) build tool
- [ReportGenerator](https://reportgenerator.io) code coverage reporting tool.
- [Azurite](https://github.com/Azure/Azurite) (for runtime - can be run via Docker)

To install Cake as a .NET tool:

```bash
dotnet tool install --global Cake.Tool
```

To install ReportGenerator as a .NET tool:

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.4.18
```

## Building the Project

This project uses [Cake](https://cakebuild.net/) for build automation. The build script provides several tasks for different scenarios.

### Quick Start

To run the default build (which performs a deep clean, build, and test):

```bash
dotnet cake
```

### Available Build Tasks

#### Build Tasks

- **Build** - Builds the solution

  ```bash
  dotnet cake --target=Build
  ```

- **Rebuild** - Performs a deep clean followed by a full build

  ```bash
  dotnet cake --target=Rebuild
  ```

#### Clean Tasks

- **Clean** - Cleans the solution output directories and artifacts

  ```bash
  dotnet cake --target=Clean
  ```

- **DeepClean** - Removes all bin, obj, artifacts, test results, and test database files

  ```bash
  dotnet cake --target=DeepClean
  ```

#### Test Tasks

- **Test** - Runs all tests with code coverage and generates reports (includes rebuild)

  ```bash
  dotnet cake --target=Test
  ```

  Test results are output to `./artifacts/test-results/` and coverage reports are generated in `./artifacts/coverage/`. The coverage summary is displayed in the console after test execution.

#### CI Task

- **CI** - Runs the full CI pipeline (deep clean, build, test, and Docker build)

  ```bash
  dotnet cake --target=CI
  ```

#### Docker Task

- **Docker** - Builds the Docker container

  ```bash
  dotnet cake --target=Docker
  ```

### Build Configuration

By default, builds use the `Release` configuration. To build in `Debug` mode:

```bash
dotnet cake --configuration=Debug
```

### Build Artifacts

Build artifacts are organized in the `./artifacts/` directory:

- `./artifacts/test-results/` - Test execution results and raw coverage data
- `./artifacts/coverage/` - Generated coverage reports (Markdown, Text Summary, and LCOV formats)

## Testing

The project includes comprehensive integration and unit tests. Tests are automatically run with code coverage collection when using the `Test`, `CI`, or `Default` tasks.

### Running Tests Manually

```bash
dotnet cake --target=Test
```

### Coverage Reports

After running tests, coverage reports are available in multiple formats:

- **Summary.txt** - Text-based coverage summary displayed in console
- **SummaryGithub.md** - GitHub-flavored markdown summary
- **lcov.info** - LCOV format for integration with coverage tools

## Development

### Project Structure

- **src/** - Source code for the Azurite UI web application
- **tests/** - Integration and unit tests
- **docs/** - Additional documentation
- **artifacts/** - Build outputs, test results, and coverage reports (generated)

## Deployment

The project includes Docker support for containerized deployment. Use Docker Compose to run Azurite UI alongside Azurite:

### Environment Configuration

The Docker Compose setup supports environment variable configuration through a `.env` file. Copy [.env.example](./.env.example) to `.env` and modify as needed:

```bash
cp .env.example .env
```

Available environment variables:

- `AZURITE_ACCOUNT_NAME` - Storage account name (default: devstoreaccount1)
- `AZURITE_ACCOUNT_KEY` - Storage account key (default: well-known Azurite key)
- `AZURITE_BLOB_PORT` - Blob service port (default: 10000)
- `AZURITEUI_PORT` - AzuriteUI external port (default: 8080)

### Docker Compose Configuration

```yaml
services:
  # Azurite - Azure Storage emulator (not exposed externally)
  azurite:
    image: mcr.microsoft.com/azure-storage/azurite:latest
    container_name: azurite
    hostname: azurite
    restart: unless-stopped
    command: "azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --loose --skipApiVersionCheck"
    volumes:
      - azurite-data:/data
    networks:
      - azurite-network
    healthcheck:
      test: nc 127.0.0.1 10000 -z
      interval: 2s
      retries: 15
    expose:
      - "10000"  # Blob service
      - "10001"  # Queue service
      - "10002"  # Table service
    environment:
      - AZURITE_ACCOUNTS=devstoreaccount1:Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==

  # AzuriteUI - Web interface (exposed externally)
  azuriteui:
    build:
      context: .
      dockerfile: Dockerfile
      args:
        BUILD_CONFIGURATION: Release
    container_name: azurite-ui
    hostname: azurite-ui
    restart: unless-stopped
    ports:
      - "8080:8080"  # Only expose UI
    depends_on:
      azurite:
        condition: service_healthy
    networks:
      - azurite-network
    volumes:
      - azurite-ui-data:/app/data  # Persist SQLite cache database
    environment:
      # Connection string pointing to internal Azurite service
      - ConnectionStrings__Azurite=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;
      - ConnectionStrings__CacheDatabase=Data Source=/app/data/cache.db;Mode=ReadWriteCreate;Cache=Shared;Foreign Keys=True;
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    healthcheck:
      test: ["CMD", "curl", "--fail", "http://localhost:8080/api/health"]
      interval: 30s
      timeout: 5s
      retries: 3
      start_period: 15s

networks:
  azurite-network:
    driver: bridge
    name: azurite-network

volumes:
  azurite-data:
    name: azurite-data
    driver: local
  azurite-ui-data:
    name: azurite-ui-data
    driver: local

```

This `docker-compose.yml` is located [in the repository](./docker-compose.yml).

Future builds will be automatically distributed to ghcr.io when GitHub releases are tagged.

## Scope

**Supported**: Blob storage container and blob operations

**Not Supported**: Azure Queue and Table storage (available in Azurite but not targeted by this UI)

## Contributing

This is a developer tool designed for local development and testing scenarios. Contributions that enhance the developer experience for working with Azurite are welcome.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

Copyright (c) 2025 Adrian Hall <photoadrian@outlook.com>
