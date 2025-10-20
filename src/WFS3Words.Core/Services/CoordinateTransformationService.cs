using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using WFS3Words.Core.Interfaces;
using WFS3Words.Core.Models;

namespace WFS3Words.Core.Services;

/// <summary>
/// Service for transforming coordinates between different coordinate reference systems.
/// </summary>
public class CoordinateTransformationService : ICoordinateTransformationService
{
    private readonly CoordinateSystemFactory _csFactory;
    private readonly CoordinateTransformationFactory _ctFactory;
    private readonly Dictionary<string, CoordinateSystem> _coordinateSystems;

    // Most commonly used CRS in GIS applications
    private static readonly string[] SupportedCodes = new[]
    {
        "EPSG:4326",  // WGS84 (lat/lon)
        "EPSG:3857",  // Web Mercator (Google Maps, OpenStreetMap)
        "EPSG:4258",  // ETRS89 (European standard)
        "EPSG:27700", // British National Grid (UK)
        "EPSG:32630", // WGS 84 / UTM zone 30N (UK, Western Europe)
        "EPSG:32631", // WGS 84 / UTM zone 31N (Central Europe)
        "EPSG:2154",  // RGF93 / Lambert-93 (France)
        "EPSG:25832", // ETRS89 / UTM zone 32N (Germany, Poland)
    };

    public IReadOnlyList<string> SupportedEpsgCodes => SupportedCodes;

    public CoordinateTransformationService()
    {
        _csFactory = new CoordinateSystemFactory();
        _ctFactory = new CoordinateTransformationFactory();
        _coordinateSystems = new Dictionary<string, CoordinateSystem>(StringComparer.OrdinalIgnoreCase);

        InitializeCoordinateSystems();
    }

    /// <inheritdoc />
    public GeoCoordinate Transform(GeoCoordinate coordinate, string targetEpsgCode)
    {
        if (!coordinate.IsValid())
        {
            throw new ArgumentException("Invalid coordinate", nameof(coordinate));
        }

        var normalizedCode = NormalizeEpsgCode(targetEpsgCode);

        // If target is WGS84, no transformation needed
        if (normalizedCode == "EPSG:4326")
        {
            return coordinate;
        }

        if (!IsSupported(normalizedCode))
        {
            throw new ArgumentException(
                $"Coordinate system {normalizedCode} is not supported. Supported systems: {string.Join(", ", SupportedEpsgCodes)}",
                nameof(targetEpsgCode));
        }

        var sourceCs = _coordinateSystems["EPSG:4326"];
        var targetCs = _coordinateSystems[normalizedCode];

        var transformation = _ctFactory.CreateFromCoordinateSystems(sourceCs, targetCs);

        // ProjNet expects [longitude, latitude] order for geographic coordinates
        var sourcePoint = new[] { coordinate.Longitude, coordinate.Latitude };
        var targetPoint = transformation.MathTransform.Transform(sourcePoint);

        // For projected systems, result is [easting, northing] or [x, y]
        // We store as latitude, longitude for consistency, but semantics change
        return new GeoCoordinate(targetPoint[1], targetPoint[0]);
    }

    /// <inheritdoc />
    public bool IsSupported(string epsgCode)
    {
        var normalized = NormalizeEpsgCode(epsgCode);
        return _coordinateSystems.ContainsKey(normalized);
    }

    /// <inheritdoc />
    public string NormalizeEpsgCode(string epsgCode)
    {
        if (string.IsNullOrWhiteSpace(epsgCode))
        {
            return "EPSG:4326"; // Default to WGS84
        }

        epsgCode = epsgCode.Trim().ToUpperInvariant();

        // Handle various input formats
        if (epsgCode.StartsWith("EPSG:"))
        {
            return epsgCode;
        }

        if (epsgCode.StartsWith("URN:OGC:DEF:CRS:EPSG::"))
        {
            var code = epsgCode.Replace("URN:OGC:DEF:CRS:EPSG::", "");
            return $"EPSG:{code}";
        }

        if (epsgCode.StartsWith("HTTP://WWW.OPENGIS.NET/DEF/CRS/EPSG/0/"))
        {
            var code = epsgCode.Replace("HTTP://WWW.OPENGIS.NET/DEF/CRS/EPSG/0/", "");
            return $"EPSG:{code}";
        }

        // Assume it's just the numeric code
        return $"EPSG:{epsgCode}";
    }

    private void InitializeCoordinateSystems()
    {
        // EPSG:4326 - WGS84
        _coordinateSystems["EPSG:4326"] = GeographicCoordinateSystem.WGS84;

        // EPSG:3857 - Web Mercator (Pseudo-Mercator)
        var webMercatorParams = new List<ProjectionParameter>
        {
            new("latitude_of_origin", 0),
            new("central_meridian", 0),
            new("false_easting", 0),
            new("false_northing", 0)
        };
        var webMercatorProjection = _csFactory.CreateProjection(
            "Popular Visualisation Pseudo Mercator",
            "Mercator_1SP",
            webMercatorParams);
        _coordinateSystems["EPSG:3857"] = _csFactory.CreateProjectedCoordinateSystem(
            "WGS 84 / Pseudo-Mercator",
            GeographicCoordinateSystem.WGS84,
            webMercatorProjection,
            LinearUnit.Metre,
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Northing", AxisOrientationEnum.North));

        // EPSG:4258 - ETRS89
        var etrs89 = _csFactory.CreateGeographicCoordinateSystem(
            "ETRS89",
            AngularUnit.Degrees,
            HorizontalDatum.ETRF89,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lat", AxisOrientationEnum.North),
            new AxisInfo("Lon", AxisOrientationEnum.East));
        _coordinateSystems["EPSG:4258"] = etrs89;

        // EPSG:27700 - British National Grid
        var bngParams = new List<ProjectionParameter>
        {
            new("latitude_of_origin", 49),
            new("central_meridian", -2),
            new("scale_factor", 0.9996012717),
            new("false_easting", 400000),
            new("false_northing", -100000)
        };
        var bngProjection = _csFactory.CreateProjection(
            "British National Grid",
            "Transverse_Mercator",
            bngParams);

        // Airy 1830 ellipsoid parameters (used by OSGB 1936)
        var airyEllipsoid = _csFactory.CreateFlattenedSphere(
            "Airy 1830",
            6377563.396,  // semi-major axis (a)
            299.3249646,  // inverse flattening (1/f)
            LinearUnit.Metre);

        var osgb36Datum = _csFactory.CreateHorizontalDatum(
            "OSGB_1936",
            DatumType.HD_Geocentric,
            airyEllipsoid,
            new Wgs84ConversionInfo(446.448, -125.157, 542.060, 0.1502, 0.2470, 0.8421, -20.4894));

        var osgb36 = _csFactory.CreateGeographicCoordinateSystem(
            "OSGB 1936",
            AngularUnit.Degrees,
            osgb36Datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lat", AxisOrientationEnum.North),
            new AxisInfo("Lon", AxisOrientationEnum.East));

        _coordinateSystems["EPSG:27700"] = _csFactory.CreateProjectedCoordinateSystem(
            "OSGB 1936 / British National Grid",
            osgb36,
            bngProjection,
            LinearUnit.Metre,
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Northing", AxisOrientationEnum.North));

        // EPSG:32630 - WGS 84 / UTM zone 30N
        _coordinateSystems["EPSG:32630"] = CreateUtmSystem(30, true);

        // EPSG:32631 - WGS 84 / UTM zone 31N
        _coordinateSystems["EPSG:32631"] = CreateUtmSystem(31, true);

        // EPSG:2154 - RGF93 / Lambert-93 (France)
        var lambert93Params = new List<ProjectionParameter>
        {
            new("latitude_of_origin", 46.5),
            new("central_meridian", 3),
            new("standard_parallel_1", 49),
            new("standard_parallel_2", 44),
            new("false_easting", 700000),
            new("false_northing", 6600000)
        };
        var lambert93Projection = _csFactory.CreateProjection(
            "Lambert-93",
            "Lambert_Conformal_Conic_2SP",
            lambert93Params);

        var rgf93 = _csFactory.CreateGeographicCoordinateSystem(
            "RGF93",
            AngularUnit.Degrees,
            HorizontalDatum.ETRF89, // RGF93 uses ETRS89/WGS84
            PrimeMeridian.Greenwich,
            new AxisInfo("Lat", AxisOrientationEnum.North),
            new AxisInfo("Lon", AxisOrientationEnum.East));

        _coordinateSystems["EPSG:2154"] = _csFactory.CreateProjectedCoordinateSystem(
            "RGF93 / Lambert-93",
            rgf93,
            lambert93Projection,
            LinearUnit.Metre,
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Northing", AxisOrientationEnum.North));

        // EPSG:25832 - ETRS89 / UTM zone 32N
        var utm32Params = new List<ProjectionParameter>
        {
            new("latitude_of_origin", 0),
            new("central_meridian", 9), // Zone 32 central meridian
            new("scale_factor", 0.9996),
            new("false_easting", 500000),
            new("false_northing", 0)
        };
        var utm32Projection = _csFactory.CreateProjection(
            "UTM zone 32N",
            "Transverse_Mercator",
            utm32Params);

        _coordinateSystems["EPSG:25832"] = _csFactory.CreateProjectedCoordinateSystem(
            "ETRS89 / UTM zone 32N",
            etrs89,
            utm32Projection,
            LinearUnit.Metre,
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Northing", AxisOrientationEnum.North));
    }

    private ProjectedCoordinateSystem CreateUtmSystem(int zone, bool isNorthernHemisphere)
    {
        var centralMeridian = -183 + (zone * 6);
        var utmParams = new List<ProjectionParameter>
        {
            new("latitude_of_origin", 0),
            new("central_meridian", centralMeridian),
            new("scale_factor", 0.9996),
            new("false_easting", 500000),
            new("false_northing", isNorthernHemisphere ? 0 : 10000000)
        };

        var projection = _csFactory.CreateProjection(
            $"UTM zone {zone}{(isNorthernHemisphere ? "N" : "S")}",
            "Transverse_Mercator",
            utmParams);

        return _csFactory.CreateProjectedCoordinateSystem(
            $"WGS 84 / UTM zone {zone}{(isNorthernHemisphere ? "N" : "S")}",
            GeographicCoordinateSystem.WGS84,
            projection,
            LinearUnit.Metre,
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Northing", AxisOrientationEnum.North));
    }
}
