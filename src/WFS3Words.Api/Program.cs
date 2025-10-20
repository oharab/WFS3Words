using WFS3Words.Core.Configuration;
using WFS3Words.Core.Interfaces;
using WFS3Words.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.Configure<What3WordsOptions>(
    builder.Configuration.GetSection(What3WordsOptions.SectionName));

builder.Services.Configure<WfsOptions>(
    builder.Configuration.GetSection(WfsOptions.SectionName));

// Add controllers
builder.Services.AddControllers();

// Register services
builder.Services.AddHttpClient<IWhat3WordsClient, What3WordsClient>();
builder.Services.AddSingleton<ICoordinateGridService, CoordinateGridService>();
builder.Services.AddSingleton<ICoordinateTransformationService, CoordinateTransformationService>();
builder.Services.AddSingleton<IWfsCapabilitiesFormatter, WfsCapabilitiesFormatter>();
builder.Services.AddSingleton<IWfsFeatureFormatter, WfsFeatureFormatter>();
builder.Services.AddSingleton<WfsQueryParser>();

// Add memory cache for future caching implementation
builder.Services.AddMemoryCache();

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() {
        Title = "WFS3Words API",
        Version = "v1",
        Description = "OGC Web Feature Service for What3Words location data"
    });
});

// Configure CORS (if needed)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add Event Log on Windows only
if (builder.Environment.IsProduction() && OperatingSystem.IsWindows())
{
    builder.Logging.AddEventLog();
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WFS3Words API v1");
    });
}

// For IIS deployment - use forwarded headers
app.UseForwardedHeaders();

app.UseCors();

app.UseRouting();

app.MapControllers();

// Root endpoint - redirect to health check
app.MapGet("/", () => Results.Redirect("/health"));

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
