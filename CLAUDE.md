# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

WFS3Words is an ASP.NET Core 8.0 web service that exposes the What3Words API through an OGC-compliant Web Feature Service (WFS) interface. This allows GIS systems and other WFS-compatible clients to query What3Words locations without requiring host system modifications.

### Key Technologies
- **Framework:** ASP.NET Core 8.0 (C#)
- **Target Platform:** Cross-platform (develop on Linux/Mac/Windows, deploy to IIS on Windows Server)
- **Testing:** xUnit
- **API:** What3Words REST API
- **Standards:** OGC WFS 1.0.0, 2.0.0, and WFS 3.0 (OGC API - Features)
- **Key Dependencies:** ProjNet (coordinate transformations), Swashbuckle (OpenAPI/Swagger)

## Project Structure

```
WFS3Words/
├── src/
│   ├── WFS3Words.Api/          # ASP.NET Core Web API project
│   │   ├── Controllers/        # WFS endpoint controllers
│   │   ├── appsettings.json    # Configuration (API keys, WFS metadata)
│   │   └── Program.cs          # Application entry point
│   └── WFS3Words.Core/         # Core business logic library
│       ├── Models/             # Domain models (W3W, WFS types)
│       ├── Services/           # What3Words client, WFS formatters
│       └── Interfaces/         # Service abstractions
├── tests/
│   ├── WFS3Words.Tests.Unit/          # Pure logic unit tests
│   └── WFS3Words.Tests.Integration/   # API integration tests
├── build.sh                    # Build script
├── test.sh                     # Test runner script
├── publish.sh                  # IIS deployment preparation
└── DEPLOYMENT.md               # Detailed IIS deployment guide
```

## Common Development Commands

### Building
```bash
# Build entire solution
dotnet build WFS3Words.sln --configuration Release

# Or use convenience script
./build.sh
```

### Testing
```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test tests/WFS3Words.Tests.Unit/WFS3Words.Tests.Unit.csproj

# Run only integration tests
dotnet test tests/WFS3Words.Tests.Integration/WFS3Words.Tests.Integration.csproj

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Or use convenience script (runs all tests with detailed output)
./test.sh
```

### Running Locally
```bash
# Run the API project
dotnet run --project src/WFS3Words.Api/WFS3Words.Api.csproj

# Run with specific environment
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/WFS3Words.Api/WFS3Words.Api.csproj

# Watch mode (auto-restart on file changes)
dotnet watch --project src/WFS3Words.Api/WFS3Words.Api.csproj

# Test the running service
curl "http://localhost:5000/health"
curl "http://localhost:5000/wfs?service=WFS&request=GetCapabilities"

# View API documentation (Development mode only)
# Navigate to: http://localhost:5000/swagger
```

### Publishing for IIS
```bash
# Create deployment package
dotnet publish src/WFS3Words.Api/WFS3Words.Api.csproj \
  --configuration Release \
  --output ./publish \
  --self-contained false

# Or use convenience script
./publish.sh
```

### Package Management
```bash
# Add NuGet package to project
dotnet add src/WFS3Words.Api/WFS3Words.Api.csproj package PackageName

# Restore packages
dotnet restore

# List outdated packages
dotnet list package --outdated
```

## Architecture and Design Principles

### Core Functionality
The service converts geographic coordinates to What3Words 3-word addresses through a WFS interface. Key features:

1. **Coordinate Grid Generation:** Creates a grid of points within a bounding box using `ICoordinateGridService`
2. **What3Words Conversion:** Converts each coordinate to a 3-word address via What3Words API
3. **Coordinate Transformation:** Transforms coordinates between different CRS using ProjNet via `ICoordinateTransformationService`
4. **WFS Response Formatting:** Formats results as GML/XML or GeoJSON using `IWfsFeatureFormatter`

Other W3W operations (reverse geocoding from words to coordinates, autosuggest) are out of scope as they don't fit the WFS paradigm, which is designed for querying features by spatial extent.

### WFS Version Support
- **WFS 1.0.0:** Classic GetCapabilities, DescribeFeatureType, GetFeature
- **WFS 2.0.0:** Enhanced query capabilities, paging
- **WFS 3.0 (OGC API - Features):** RESTful JSON API

### Separation of Concerns
- **WFS3Words.Api:** HTTP endpoints, request/response handling, WFS protocol implementation
- **WFS3Words.Core:** Business logic, What3Words API client, data transformation, WFS response formatting

### Dependency Philosophy
Minimize external dependencies while maintaining code readability. Use built-in .NET libraries where possible:
- **HTTP Client:** `HttpClient` (built-in)
- **JSON:** `System.Text.Json` (built-in)
- **Logging:** `Microsoft.Extensions.Logging` (built-in)
- **Configuration:** `Microsoft.Extensions.Configuration` (built-in)
- **Coordinate Transformations:** `ProjNet` (external - required for CRS support)

### Dependency Injection
Services are registered in `src/WFS3Words.Api/Program.cs`:
- `IWhat3WordsClient` - HTTP client for What3Words API (scoped via HttpClientFactory)
- `ICoordinateGridService` - Generates coordinate grids from bounding boxes (singleton)
- `ICoordinateTransformationService` - Transforms coordinates between CRS (singleton)
- `IWfsCapabilitiesFormatter` - Formats WFS GetCapabilities responses (singleton)
- `IWfsFeatureFormatter` - Formats WFS GetFeature responses (singleton)
- `WfsQueryParser` - Parses WFS query parameters (singleton)

### Configuration
API keys and settings are stored in `appsettings.json`:
```json
{
  "What3Words": {
    "ApiKey": "YOUR_API_KEY"
  },
  "WFS": {
    "ServiceTitle": "What3Words WFS Service",
    "ServiceAbstract": "...",
    // ... other WFS metadata
  }
}
```

**IMPORTANT:** Never commit real API keys. Use `appsettings.Development.json` (gitignored) for local development.

## Testing Strategy

### Unit Tests (`WFS3Words.Tests.Unit`)
- Test pure business logic in `WFS3Words.Core`
- No database, no HTTP calls, no external dependencies
- Mock interfaces for What3Words API client
- Located in `tests/WFS3Words.Tests.Unit/`

### Integration Tests (`WFS3Words.Tests.Integration`)
- Test full HTTP request/response cycle using `Microsoft.AspNetCore.Mvc.Testing`
- Use `WebApplicationFactory<T>` to host the API in-memory
- Test WFS endpoints with real request formats
- May use test API key or mock HTTP responses
- Located in `tests/WFS3Words.Tests.Integration/`

### Test Naming Convention
Follow xUnit `[Fact]` and `[Theory]` patterns with descriptive names:
```csharp
[Fact]
public void ConvertCoordinatesToW3W_ShouldReturnValidThreeWordAddress_WhenCoordinatesAreValid()
{
    // Arrange, Act, Assert
}
```

## IIS Deployment

This application is designed to run on IIS on Windows Server. See `DEPLOYMENT.md` for comprehensive deployment instructions.

**Quick deployment checklist:**
1. Install .NET 8.0 Hosting Bundle on Windows Server
2. Create IIS Application Pool (No Managed Code, Integrated pipeline)
3. Run `./publish.sh` to create deployment package
4. Copy `publish/` contents to IIS physical path
5. Configure What3Words API key in `appsettings.json` or environment variables
6. Start application pool and test

## API Endpoints

### Health Check
```
GET /health
```
Returns HTTP 200 if the service is running. Used for monitoring and load balancer health checks.

### WFS 1.0.0 / 2.0.0 (Query String)
```
GET /wfs?service=WFS&version=1.0.0&request=GetCapabilities
GET /wfs?service=WFS&version=1.0.0&request=DescribeFeatureType&typeName=w3w:location
GET /wfs?service=WFS&version=1.0.0&request=GetFeature&typeName=w3w:location&BBOX=...
```

**Supported DescribeFeatureType outputFormat values:**
- `XMLSCHEMA` (default)
- `text/xml; subtype=gml/3.1.1`
- `text/xml; subtype=gml/3.2.0`

**Supported GetFeature output formats:**
- `application/gml+xml` (default)
- `application/json` (GeoJSON)

**Supported Coordinate Reference Systems (via srsName or srs parameter):**
- `EPSG:4326` - WGS84 (default)
- `EPSG:3857` - Web Mercator
- `EPSG:4258` - ETRS89
- `EPSG:27700` - British National Grid
- `EPSG:32630`, `EPSG:32631` - UTM zones
- `EPSG:2154` - Lambert-93 (France)
- `EPSG:25832` - ETRS89 / UTM zone 32N

### WFS 3.0 / OGC API - Features (RESTful)
```
GET /collections
GET /collections/locations
GET /collections/locations/items?bbox=...
```

### Swagger/OpenAPI (Development Only)
```
GET /swagger
```
Interactive API documentation available when running in Development mode.

## Code Style and Conventions

Follow standard C# conventions:
- PascalCase for public members, classes, methods
- camelCase for private fields, parameters
- Use `async`/`await` for I/O operations
- Prefer `record` types for DTOs and immutable data models
- Use nullable reference types (`#nullable enable`)

## Important Notes

- **API Key Security:** Always use environment variables or secure vaults in production, never hardcode
- **Logging:** Use `ILogger<T>` dependency injection for structured logging
- **Error Handling:** Return appropriate WFS exception reports (XML) for WFS 1.0/2.0 or JSON problem details for WFS 3.0
- **Performance:** Consider caching WFS GetCapabilities responses
- **CORS:** May need to enable CORS for browser-based WFS clients

## Development Workflow

1. **Create feature branch** from main
2. **Write failing test first** (TDD approach per CLAUDE.md best practices)
3. **Implement minimal code** to pass the test
4. **Run all tests** to ensure no regressions
5. **Test locally** using `dotnet run`
6. **Commit with conventional commits** format
7. **Create pull request** for review

## Useful Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [What3Words API Docs](https://developer.what3words.com/public-api)
- [OGC WFS Standards](https://www.ogc.org/standards/wfs)
- [IIS Hosting Guide](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/)
