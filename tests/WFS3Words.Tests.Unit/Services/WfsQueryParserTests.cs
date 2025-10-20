using Microsoft.Extensions.Primitives;
using WFS3Words.Core.Services;

namespace WFS3Words.Tests.Unit.Services;

public class WfsQueryParserTests
{
    private readonly WfsQueryParser _parser;

    public WfsQueryParserTests()
    {
        _parser = new WfsQueryParser();
    }

    [Fact]
    public void Parse_ShouldExtractService_WhenProvided()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["service"] = "WFS"
        };

        var result = _parser.Parse(queryParams);

        Assert.Equal("WFS", result.Service);
    }

    [Fact]
    public void Parse_ShouldExtractVersion_WhenProvided()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["version"] = "2.0.0"
        };

        var result = _parser.Parse(queryParams);

        Assert.Equal("2.0.0", result.Version);
    }

    [Fact]
    public void Parse_ShouldExtractRequest_WhenProvided()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["request"] = "GetCapabilities"
        };

        var result = _parser.Parse(queryParams);

        Assert.Equal("GetCapabilities", result.Request);
    }

    [Fact]
    public void Parse_ShouldBeCaseInsensitive()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["SERVICE"] = "WFS",
            ["VERSION"] = "2.0.0",
            ["REQUEST"] = "GetFeature"
        };

        var result = _parser.Parse(queryParams);

        Assert.Equal("WFS", result.Service);
        Assert.Equal("2.0.0", result.Version);
        Assert.Equal("GetFeature", result.Request);
    }

    [Fact]
    public void Parse_ShouldExtractTypeName_WhenProvided()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["typename"] = "w3w:location"
        };

        var result = _parser.Parse(queryParams);

        Assert.Equal("w3w:location", result.TypeName);
    }

    [Fact]
    public void Parse_ShouldExtractTypeNames_WhenTypeNameNotProvided()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["typenames"] = "w3w:location"
        };

        var result = _parser.Parse(queryParams);

        Assert.Equal("w3w:location", result.TypeName);
    }

    [Fact]
    public void Parse_ShouldExtractBoundingBox_WhenValidBBoxProvided()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["bbox"] = "-1,51,0,52"
        };

        var result = _parser.Parse(queryParams);

        Assert.NotNull(result.BBox);
        Assert.Equal(51.0, result.BBox.MinLatitude);
        Assert.Equal(-1.0, result.BBox.MinLongitude);
        Assert.Equal(52.0, result.BBox.MaxLatitude);
        Assert.Equal(0.0, result.BBox.MaxLongitude);
    }

    [Fact]
    public void Parse_ShouldReturnNullBBox_WhenInvalidBBoxProvided()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["bbox"] = "invalid"
        };

        var result = _parser.Parse(queryParams);

        Assert.Null(result.BBox);
    }

    [Fact]
    public void Parse_ShouldExtractMaxFeatures_WhenProvided()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["maxfeatures"] = "100"
        };

        var result = _parser.Parse(queryParams);

        Assert.Equal(100, result.MaxFeatures);
    }

    [Fact]
    public void Parse_ShouldExtractCount_WhenMaxFeaturesNotProvided()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["count"] = "50"
        };

        var result = _parser.Parse(queryParams);

        Assert.Equal(50, result.MaxFeatures);
    }

    [Fact]
    public void Parse_ShouldReturnNullMaxFeatures_WhenInvalidNumberProvided()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["maxfeatures"] = "not-a-number"
        };

        var result = _parser.Parse(queryParams);

        Assert.Null(result.MaxFeatures);
    }

    [Fact]
    public void Parse_ShouldExtractOutputFormat_WhenProvided()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["outputformat"] = "application/json"
        };

        var result = _parser.Parse(queryParams);

        Assert.Equal("application/json", result.OutputFormat);
    }

    [Fact]
    public void Parse_ShouldHandleCompleteWfsRequest()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["service"] = "WFS",
            ["version"] = "2.0.0",
            ["request"] = "GetFeature",
            ["typename"] = "w3w:location",
            ["bbox"] = "-1,51,0,52",
            ["maxfeatures"] = "100",
            ["outputformat"] = "gml3"
        };

        var result = _parser.Parse(queryParams);

        Assert.Equal("WFS", result.Service);
        Assert.Equal("2.0.0", result.Version);
        Assert.Equal("GetFeature", result.Request);
        Assert.Equal("w3w:location", result.TypeName);
        Assert.NotNull(result.BBox);
        Assert.Equal(100, result.MaxFeatures);
        Assert.Equal("gml3", result.OutputFormat);
    }

    [Fact]
    public void Parse_ShouldHandleEmptyQueryParams()
    {
        var queryParams = new Dictionary<string, StringValues>();

        var result = _parser.Parse(queryParams);

        Assert.Null(result.Service);
        Assert.Null(result.Version);
        Assert.Null(result.Request);
        Assert.Null(result.TypeName);
        Assert.Null(result.BBox);
        Assert.Null(result.MaxFeatures);
        Assert.Null(result.OutputFormat);
    }

    [Fact]
    public void Parse_ShouldIgnoreWhitespaceValues()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["service"] = "  ",
            ["version"] = ""
        };

        var result = _parser.Parse(queryParams);

        Assert.Null(result.Service);
        Assert.Null(result.Version);
    }

    [Fact]
    public void Parse_ShouldExtractSrsName_WhenProvided()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["srsname"] = "EPSG:3857"
        };

        var result = _parser.Parse(queryParams);

        Assert.Equal("EPSG:3857", result.SrsName);
    }

    [Fact]
    public void Parse_ShouldExtractSrs_WhenSrsNameNotProvided()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["srs"] = "EPSG:27700"
        };

        var result = _parser.Parse(queryParams);

        Assert.Equal("EPSG:27700", result.SrsName);
    }

    [Theory]
    [InlineData("EPSG:3857")]
    [InlineData("epsg:3857")]
    [InlineData("EPSG:27700")]
    public void Parse_ShouldHandleSrsNameCaseInsensitive(string srsValue)
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["SRSNAME"] = srsValue
        };

        var result = _parser.Parse(queryParams);

        Assert.Equal(srsValue, result.SrsName);
    }
}
