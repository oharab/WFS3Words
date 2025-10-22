# WFS3Words

An ASP.NET Core web service that exposes the What3Words API through an OGC-compliant Web Feature Service (WFS) interface.

## What is WFS3Words?

WFS3Words acts as a bridge between the What3Words geocoding service and GIS systems that support OGC Web Feature Service standards. This allows systems that can query WFS endpoints to access What3Words location data without requiring modifications to the host system.

## Features

- **OGC WFS Compliant:** Supports WFS 1.0.0, 2.0.0, and WFS 3.0 (OGC API - Features)
- **What3Words Integration:** Converts geographic coordinates to 3-word addresses
- **Multiple Coordinate Systems:** Supports coordinate transformations to 8 different CRS including Web Mercator, British National Grid, and UTM zones
- **Cross-platform Development:** Built with .NET 8.0, develop on Linux/Mac/Windows
- **IIS Ready:** Optimized for deployment to IIS on Windows Server
- **Standards-based:** Fully compliant with OGC WFS specifications

## Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- What3Words API Key ([sign up here](https://what3words.com/select-plan))

### Setup

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd WFS3Words
   ```

2. **Configure API Key:**

   Edit `src/WFS3Words.Api/appsettings.Development.json`:
   ```json
   {
     "What3Words": {
       "ApiKey": "YOUR_WHAT3WORDS_API_KEY"
     }
   }
   ```

3. **Build the project:**
   ```bash
   ./build.sh
   # Or: dotnet build
   ```

4. **Run tests:**
   ```bash
   ./test.sh
   # Or: dotnet test
   ```

5. **Run locally:**
   ```bash
   dotnet run --project src/WFS3Words.Api/WFS3Words.Api.csproj
   ```

6. **Test the service:**
   ```bash
   curl "http://localhost:5000/wfs?service=WFS&request=GetCapabilities"
   ```

## Deployment to IIS

For detailed deployment instructions, see [DEPLOYMENT.md](DEPLOYMENT.md).

**Quick summary:**
1. Install .NET 8.0 Hosting Bundle on Windows Server
2. Run `./publish.sh` to create deployment package
3. Copy to IIS and configure application pool
4. Set API key and start the service

## Development

See [CLAUDE.md](CLAUDE.md) for detailed development guidelines and architecture documentation.

### Common Commands

```bash
# Build
./build.sh

# Test
./test.sh

# Run with hot reload
dotnet watch --project src/WFS3Words.Api/WFS3Words.Api.csproj

# Publish for IIS
./publish.sh
```

## Project Structure

```
WFS3Words/
├── src/
│   ├── WFS3Words.Api/      # Web API project
│   └── WFS3Words.Core/     # Core business logic
├── tests/
│   ├── WFS3Words.Tests.Unit/        # Unit tests
│   └── WFS3Words.Tests.Integration/ # Integration tests
├── CLAUDE.md               # Development guide
├── DEPLOYMENT.md           # IIS deployment guide
└── README.md               # This file
```

## WFS Endpoints

### GetCapabilities
Returns service metadata and supported operations.
```
GET /wfs?service=WFS&request=GetCapabilities&version=1.0.0
```

### DescribeFeatureType
Describes the structure of feature types.
```
GET /wfs?service=WFS&request=DescribeFeatureType&typeName=w3w:location
```

**Optional Parameters:**
- `outputFormat`: Schema description language (default: XMLSCHEMA)

**Supported Output Formats:**
- `XMLSCHEMA` - XML Schema Definition (default)
- `text/xml; subtype=gml/3.1.1` - GML 3.1.1 Schema
- `text/xml; subtype=gml/3.2.0` - GML 3.2.0 Schema

**Example with outputFormat:**
```
GET /wfs?service=WFS&request=DescribeFeatureType&typeName=w3w:location&outputFormat=XMLSCHEMA
```

### GetFeature
Retrieves What3Words locations for a bounding box.
```
GET /wfs?service=WFS&request=GetFeature&typeName=w3w:location&BBOX=minx,miny,maxx,maxy
```

**Optional Parameters:**
- `srsName` or `srs`: Target coordinate reference system (e.g., `EPSG:3857`)
- `outputFormat`: Output format (`application/gml+xml` or `application/json`)

**Supported Coordinate Systems:**
- `EPSG:4326` - WGS84 (default)
- `EPSG:3857` - Web Mercator (Google Maps, OpenStreetMap)
- `EPSG:4258` - ETRS89 (European standard)
- `EPSG:27700` - British National Grid (UK)
- `EPSG:32630` - WGS 84 / UTM zone 30N (UK, Western Europe)
- `EPSG:32631` - WGS 84 / UTM zone 31N (Central Europe)
- `EPSG:2154` - RGF93 / Lambert-93 (France)
- `EPSG:25832` - ETRS89 / UTM zone 32N (Germany, Poland)

**Example with coordinate transformation:**
```
GET /wfs?service=WFS&request=GetFeature&typeName=w3w:location&BBOX=-1,51,0,52&srsName=EPSG:3857
```

## License

[Add your license here]

## Contributing

[Add contribution guidelines here]

## Support

For issues and questions, please open an issue in the repository.

## Acknowledgments

- [What3Words](https://what3words.com/) for their geocoding API
- [Open Geospatial Consortium](https://www.ogc.org/) for WFS standards
