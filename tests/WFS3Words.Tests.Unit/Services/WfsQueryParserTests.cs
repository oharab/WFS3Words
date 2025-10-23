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

    [Fact]
    public void Parse_ShouldExtractBBoxFromFilter_WithGml2Coordinates()
    {
        var filterXml = @"<Filter xmlns=""http://www.opengis.net/ogc"">
            <BBOX>
                <PropertyName>geometry</PropertyName>
                <gml:Box xmlns:gml=""http://www.opengis.net/gml"" srsName=""EPSG:27700"">
                    <gml:coordinates>313713.85,385205.17 469814.49,483863.37</gml:coordinates>
                </gml:Box>
            </BBOX>
        </Filter>";

        var queryParams = new Dictionary<string, StringValues>
        {
            ["filter"] = filterXml
        };

        var result = _parser.Parse(queryParams);

        Assert.NotNull(result.BBox);
        Assert.Equal(385205.17, result.BBox.MinLatitude);
        Assert.Equal(313713.85, result.BBox.MinLongitude);
        Assert.Equal(483863.37, result.BBox.MaxLatitude);
        Assert.Equal(469814.49, result.BBox.MaxLongitude);
    }

    [Fact]
    public void Parse_ShouldExtractBBoxFromFilter_WithGml3Envelope()
    {
        var filterXml = @"<Filter xmlns=""http://www.opengis.net/ogc"">
            <BBOX>
                <PropertyName>geometry</PropertyName>
                <gml:Envelope xmlns:gml=""http://www.opengis.net/gml"" srsName=""EPSG:4326"">
                    <gml:lowerCorner>-1.0 51.0</gml:lowerCorner>
                    <gml:upperCorner>0.0 52.0</gml:upperCorner>
                </gml:Envelope>
            </BBOX>
        </Filter>";

        var queryParams = new Dictionary<string, StringValues>
        {
            ["filter"] = filterXml
        };

        var result = _parser.Parse(queryParams);

        Assert.NotNull(result.BBox);
        Assert.Equal(51.0, result.BBox.MinLatitude);
        Assert.Equal(-1.0, result.BBox.MinLongitude);
        Assert.Equal(52.0, result.BBox.MaxLatitude);
        Assert.Equal(0.0, result.BBox.MaxLongitude);
    }

    [Fact]
    public void Parse_ShouldPreferDirectBBox_OverFilterBBox()
    {
        var filterXml = @"<Filter xmlns=""http://www.opengis.net/ogc"">
            <BBOX>
                <gml:Box xmlns:gml=""http://www.opengis.net/gml"">
                    <gml:coordinates>0,0 1,1</gml:coordinates>
                </gml:Box>
            </BBOX>
        </Filter>";

        var queryParams = new Dictionary<string, StringValues>
        {
            ["bbox"] = "-1,51,0,52",
            ["filter"] = filterXml
        };

        var result = _parser.Parse(queryParams);

        Assert.NotNull(result.BBox);
        // Should use direct BBOX parameter
        Assert.Equal(51.0, result.BBox.MinLatitude);
        Assert.Equal(-1.0, result.BBox.MinLongitude);
    }

    [Fact]
    public void Parse_ShouldReturnNullBBox_WhenFilterIsInvalidXml()
    {
        var queryParams = new Dictionary<string, StringValues>
        {
            ["filter"] = "<invalid xml"
        };

        var result = _parser.Parse(queryParams);

        Assert.Null(result.BBox);
    }

    [Fact]
    public void Parse_ShouldReturnNullBBox_WhenFilterHasNoBBoxElement()
    {
        var filterXml = @"<Filter xmlns=""http://www.opengis.net/ogc"">
            <PropertyIsEqualTo>
                <PropertyName>name</PropertyName>
                <Literal>test</Literal>
            </PropertyIsEqualTo>
        </Filter>";

        var queryParams = new Dictionary<string, StringValues>
        {
            ["filter"] = filterXml
        };

        var result = _parser.Parse(queryParams);

        Assert.Null(result.BBox);
    }

    [Fact]
    public void Parse_ShouldHandleFilterWithoutNamespace()
    {
        var filterXml = @"<Filter xmlns=""http://www.opengis.net/ogc"">
            <BBOX>
                <gml:Box xmlns:gml=""http://www.opengis.net/gml"">
                    <gml:coordinates>-1,51 0,52</gml:coordinates>
                </gml:Box>
            </BBOX>
        </Filter>";

        var queryParams = new Dictionary<string, StringValues>
        {
            ["filter"] = filterXml
        };

        var result = _parser.Parse(queryParams);

        Assert.NotNull(result.BBox);
        Assert.Equal(51.0, result.BBox.MinLatitude);
        Assert.Equal(-1.0, result.BBox.MinLongitude);
    }

    [Fact]
    public void Parse_ShouldHandleRealWorldGisClientFilter()
    {
        // This is the actual filter from GitHub issue #6
        // Note: EPSG:27700 (British National Grid) uses meters, not degrees
        var filterXml = @"<Filter xmlns=""http://www.opengis.net/ogc""><BBOX><PropertyName>geometry</PropertyName><gml:Box xmlns:gml=""http://www.opengis.net/gml"" srsName=""EPSG:27700""><gml:coordinates>313713.8465717831,385205.1723566118 469814.4853875725,483863.37235661177</gml:coordinates></gml:Box></BBOX></Filter>";

        var queryParams = new Dictionary<string, StringValues>
        {
            ["request"] = "GetFeature",
            ["version"] = "1.0.0",
            ["service"] = "WFS",
            ["typename"] = "location",
            ["filter"] = filterXml
        };

        var result = _parser.Parse(queryParams);

        Assert.NotNull(result.BBox);
        // Verify the BBOX was extracted correctly (coordinates in meters for EPSG:27700)
        Assert.InRange(result.BBox.MinLongitude, 313000, 314000);
        Assert.InRange(result.BBox.MinLatitude, 385000, 386000);
        Assert.InRange(result.BBox.MaxLongitude, 469000, 470000);
        Assert.InRange(result.BBox.MaxLatitude, 483000, 484000);
        // Note: IsValid() will return false for projected coordinates (not lat/lon)
        // but that's okay - coordinate transformation will handle this
    }
}
