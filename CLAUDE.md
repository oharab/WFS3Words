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

# Run specific test by name pattern
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Run specific test by display name (examples)
dotnet test --filter "DisplayName~GetCapabilities"
dotnet test --filter "DisplayName~XSD"

# Or use convenience script (runs unit tests, then integration tests, both with detailed output)
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
5. **XSD Validation:** WFS responses are validated against OGC XSD schemas to ensure compliance

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
- **CRITICAL:** No database, no HTTP calls, no external dependencies
- Mock interfaces for What3Words API client
- Located in `tests/WFS3Words.Tests.Unit/`
- Examples: coordinate transformations, grid generation, WFS formatting logic

### Integration Tests (`WFS3Words.Tests.Integration`)
- Test full HTTP request/response cycle using `Microsoft.AspNetCore.Mvc.Testing`
- Use `WebApplicationFactory<T>` to host the API in-memory
- Test WFS endpoints with real request formats
- Include XSD validation tests for WFS GetCapabilities and GetFeature responses
- May use test API key or mock HTTP responses
- Located in `tests/WFS3Words.Tests.Integration/`

### Test Separation (IMPORTANT)
**Always maintain strict separation between unit and integration tests:**
- Pure logic → `WFS3Words.Tests.Unit`
- HTTP/API/External dependencies → `WFS3Words.Tests.Integration`

This separation allows running fast unit tests during development and slower integration tests during CI/CD.

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

**GetFeature BBOX Parameter:**

BBOX can be specified in two ways:

1. **Direct parameter** (simple): `BBOX=minx,miny,maxx,maxy`
   ```
   /wfs?request=GetFeature&typeName=w3w:location&BBOX=-1,51,0,52
   ```

2. **OGC Filter XML** (advanced): `filter=<Filter>...</Filter>`
   ```
   /wfs?request=GetFeature&typeName=w3w:location&filter=<Filter xmlns="http://www.opengis.net/ogc">
     <BBOX>
       <PropertyName>geometry</PropertyName>
       <gml:Box xmlns:gml="http://www.opengis.net/gml">
         <gml:coordinates>-1,51 0,52</gml:coordinates>
       </gml:Box>
     </BBOX>
   </Filter>
   ```

**Supported Filter formats:**
- GML 2.x: `<gml:Box><gml:coordinates>x1,y1 x2,y2</gml:coordinates></gml:Box>`
- GML 3.x: `<gml:Envelope><gml:lowerCorner>x1 y1</gml:lowerCorner><gml:upperCorner>x2 y2</gml:upperCorner></gml:Envelope>`

**Coordinate Reference System Support:**

**Currently Supported (WGS84 only):**
- `EPSG:4326` - WGS84 (decimal degrees latitude/longitude)

**Future Support (planned):**
Coordinate transformation from projected CRS to WGS84 is planned for:
- `EPSG:3857` - Web Mercator
- `EPSG:4258` - ETRS89
- `EPSG:27700` - British National Grid
- `EPSG:32630`, `EPSG:32631` - UTM zones
- `EPSG:2154` - Lambert-93 (France)
- `EPSG:25832` - ETRS89 / UTM zone 32N

**Important:** Coordinates must currently be provided in WGS84 (EPSG:4326) decimal degrees. Projected coordinate systems are not yet supported. If you provide coordinates in a projected CRS, the service will return an error explaining that only WGS84 is currently supported.

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

## Logging Configuration

The service includes comprehensive logging for monitoring and debugging:

### Logging Levels

- **Information:** Request start/completion, WFS operations, feature counts, health check results
- **Debug:** Detailed request/response data, API calls, coordinate generation, response sizes
- **Warning:** Failed operations, invalid parameters, API connectivity issues
- **Error:** Exceptions, API failures, processing errors

### Request Logging Middleware

Automatically logs all HTTP requests via `RequestLoggingMiddleware`:
- Request method, path, query string with unique request ID
- Request headers (Debug level)
- POST body content (automatically truncated if > 4000 bytes)
- Response status code and elapsed time in milliseconds

**Example log output:**
```
info: [abc123de] GET /wfs?service=WFS&request=GetFeature&BBOX=-1,51,0,52 - Started
dbug: [abc123de] Headers: Host=localhost, Accept=application/xml
info: WFS GetFeature request (version 2.0.0) from 127.0.0.1
info: GetFeature request: BBOX=[-1,51,0,52], SRS=EPSG:4326, OutputFormat=GML
dbug: Generated 25 coordinates for BBOX [-1,51,0,52]
info: Returning 25 features for GetFeature request
info: [abc123de] GET /wfs?service=WFS&request=GetFeature&BBOX=-1,51,0,52 - Completed with 200 in 1250ms
```

### Logging Providers

Configured in `Program.cs`:
- **Console:** All environments
- **Debug:** All environments (for development debugging)
- **Windows Event Log:** Production only (IIS deployments)

### Adjusting Log Levels

Edit `appsettings.json` to control verbosity:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "WFS3Words.Api.Middleware.RequestLoggingMiddleware": "Information",
      "WFS3Words.Api.Controllers": "Information",
      "WFS3Words.Core.Services": "Debug"
    }
  }
}
```

**Recommended settings:**
- **Development:** `Default: Debug` for detailed diagnostics
- **Production:** `Default: Information` to reduce log volume
- **Troubleshooting:** Set specific namespaces to `Debug` or `Trace`

### IIS Event Log

In production on Windows Server, logs are written to Windows Event Log under "Application" source. View using Event Viewer.

## Important Notes

- **API Key Security:** Always use environment variables or secure vaults in production, never hardcode
- **Logging:** Use `ILogger<T>` dependency injection for structured logging with named parameters
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
