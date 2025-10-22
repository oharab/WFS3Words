using System.Text.Json;
using WFS3Words.Core.Models;
using WFS3Words.Core.Services;

namespace WFS3Words.Tests.Unit.Services;

public class WfsFeatureFormatterTests
{
    private readonly WfsFeatureFormatter _formatter;
    private readonly WfsFeatureCollection _sampleCollection;

    public WfsFeatureFormatterTests()
    {
        var transformationService = new CoordinateTransformationService();
        _formatter = new WfsFeatureFormatter(transformationService);

        var coordinate = new GeoCoordinate(51.520847, -0.195521);
        var square = new BoundingBox(51.520833, -0.195543, 51.520861, -0.195499);
        var location = new What3WordsLocation(
            Words: "filled.count.soap",
            Coordinates: coordinate,
            Country: "GB",
            Square: square,
            NearestPlace: "Bayswater, London",
            Language: "en",
            Map: "https://w3w.co/filled.count.soap");

        var feature = new WfsFeature("feature-1", coordinate, location);

        _sampleCollection = new WfsFeatureCollection(
            Features: new[] { feature },
            TotalCount: 1,
            BoundingBox: square);
    }

    [Fact]
    public void FormatAsGml_ShouldReturnValidXml_WhenCalled()
    {
        var result = _formatter.FormatAsGml(_sampleCollection);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("<?xml", result);
        Assert.Contains("FeatureCollection", result);
    }

    [Fact]
    public void FormatAsGml_ShouldIncludeNumberOfFeatures()
    {
        var result = _formatter.FormatAsGml(_sampleCollection);

        Assert.Contains("numberOfFeatures=\"1\"", result);
    }

    [Fact]
    public void FormatAsGml_ShouldIncludeFeatureData()
    {
        var result = _formatter.FormatAsGml(_sampleCollection);

        Assert.Contains("filled.count.soap", result);
        Assert.Contains("GB", result);
        Assert.Contains("Bayswater, London", result);
    }

    [Fact]
    public void FormatAsGml_ShouldIncludeGeometry()
    {
        var result = _formatter.FormatAsGml(_sampleCollection);

        Assert.Contains("Point", result);
        Assert.Contains("51.520847", result);
        Assert.Contains("-0.195521", result);
    }

    [Fact]
    public void FormatAsGml_ShouldIncludeBoundingBox_WhenProvided()
    {
        var result = _formatter.FormatAsGml(_sampleCollection);

        Assert.Contains("boundedBy", result);
        Assert.Contains("Envelope", result);
    }

    [Fact]
    public void FormatAsGml_ShouldIncludeNamespaces()
    {
        var result = _formatter.FormatAsGml(_sampleCollection);

        Assert.Contains("xmlns:wfs", result);
        Assert.Contains("xmlns:gml", result);
        Assert.Contains("xmlns:w3w", result);
    }

    [Fact]
    public void FormatAsGml_ShouldUseGml2Coordinates_ForWfs10()
    {
        var version = "1.0.0";

        var result = _formatter.FormatAsGml(_sampleCollection, version);

        // GML 2 uses <gml:coordinates> with comma-separated values
        Assert.Contains("<gml:coordinates", result);
        Assert.DoesNotContain("<gml:pos", result);
    }

    [Fact]
    public void FormatAsGml_ShouldUseGml3Pos_ForWfs20()
    {
        var version = "2.0.0";

        var result = _formatter.FormatAsGml(_sampleCollection, version);

        // GML 3 uses <gml:pos> with space-separated values
        Assert.Contains("<gml:pos", result);
        Assert.DoesNotContain("<gml:coordinates", result);
    }

    [Fact]
    public void FormatAsGml_ShouldUseBox_ForWfs10()
    {
        var version = "1.0.0";

        var result = _formatter.FormatAsGml(_sampleCollection, version);

        // GML 2 uses Box for bounding boxes
        Assert.Contains("<gml:Box", result);
        Assert.DoesNotContain("<gml:Envelope", result);
    }

    [Fact]
    public void FormatAsGml_ShouldUseEnvelope_ForWfs20()
    {
        var version = "2.0.0";

        var result = _formatter.FormatAsGml(_sampleCollection, version);

        // GML 3 uses Envelope for bounding boxes
        Assert.Contains("<gml:Envelope", result);
        Assert.Contains("<gml:lowerCorner", result);
        Assert.Contains("<gml:upperCorner", result);
        Assert.DoesNotContain("<gml:Box", result);
    }

    [Fact]
    public void FormatAsGeoJson_ShouldReturnValidJson_WhenCalled()
    {
        var result = _formatter.FormatAsGeoJson(_sampleCollection);

        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify it's valid JSON
        var doc = JsonDocument.Parse(result);
        Assert.NotNull(doc);
    }

    [Fact]
    public void FormatAsGeoJson_ShouldIncludeFeatureCollectionType()
    {
        var result = _formatter.FormatAsGeoJson(_sampleCollection);

        var doc = JsonDocument.Parse(result);
        var type = doc.RootElement.GetProperty("type").GetString();

        Assert.Equal("FeatureCollection", type);
    }

    [Fact]
    public void FormatAsGeoJson_ShouldIncludeFeatureProperties()
    {
        var result = _formatter.FormatAsGeoJson(_sampleCollection);

        Assert.Contains("filled.count.soap", result);
        Assert.Contains("GB", result);
        Assert.Contains("Bayswater, London", result);
    }

    [Fact]
    public void FormatAsGeoJson_ShouldIncludeGeometry()
    {
        var result = _formatter.FormatAsGeoJson(_sampleCollection);

        var doc = JsonDocument.Parse(result);
        var feature = doc.RootElement.GetProperty("features")[0];
        var geometry = feature.GetProperty("geometry");

        Assert.Equal("Point", geometry.GetProperty("type").GetString());

        var coordinates = geometry.GetProperty("coordinates");
        Assert.Equal(-0.195521, coordinates[0].GetDouble(), precision: 6);
        Assert.Equal(51.520847, coordinates[1].GetDouble(), precision: 6);
    }

    [Fact]
    public void FormatAsGeoJson_ShouldIncludeFeatureCount()
    {
        var result = _formatter.FormatAsGeoJson(_sampleCollection);

        var doc = JsonDocument.Parse(result);

        Assert.Equal(1, doc.RootElement.GetProperty("numberMatched").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("numberReturned").GetInt32());
    }

    [Fact]
    public void GenerateFeatureTypeDescription_ShouldReturnValidXml()
    {
        var result = _formatter.GenerateFeatureTypeDescription();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("<?xml", result);
        Assert.Contains("schema", result);
    }

    [Fact]
    public void GenerateFeatureTypeDescription_ShouldIncludeLocationFeatureType()
    {
        var result = _formatter.GenerateFeatureTypeDescription();

        Assert.Contains("locationType", result);
        Assert.Contains("location", result);
    }

    [Fact]
    public void GenerateFeatureTypeDescription_ShouldIncludeAllProperties()
    {
        var result = _formatter.GenerateFeatureTypeDescription();

        Assert.Contains("words", result);
        Assert.Contains("country", result);
        Assert.Contains("nearestPlace", result);
        Assert.Contains("language", result);
        Assert.Contains("geometry", result);
    }
}
