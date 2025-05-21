using ApiAggregation.Services;
using ApiAggregation.Models;
using ApiAggregation.Middleware;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreRateLimit;
using Serilog;
using Microsoft.OpenApi.Models;
using System.Reflection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { 
        Title = "API Aggregation", 
        Version = "v1",
        Description = "An API aggregation service that combines multiple external APIs"
    });
    
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProvider =>
        tracerProvider
            .AddSource("ApiAggregation")
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("ApiAggregation"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter())
    .WithMetrics(metricsProvider =>
        metricsProvider
            .AddMeter("ApiAggregation")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter());

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Add Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString");
});

// Add custom services
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddSingleton<CircuitBreakerService>();
builder.Services.AddSingleton<MetricsService>();
builder.Services.AddSingleton<ApiThrottlingService>();
builder.Services.AddHostedService<BackgroundJobService>();

// Add Response Caching and Compression
builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddRedis(builder.Configuration.GetValue<string>("Redis:ConnectionString")!)
    .AddUrlGroup(new Uri(builder.Configuration["GitHub:ApiBaseUrl"]!), "GitHub API")
    .AddUrlGroup(new Uri(builder.Configuration["OpenWeatherMap:ApiBaseUrl"]!), "OpenWeatherMap API");

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        builder =>
        {
            builder.WithOrigins("http://localhost:3000")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

// Configure API services
var gitHubSettings = new GitHubApiSettings
{
    AccessToken = builder.Configuration["GitHub:AccessToken"],
    ApiBaseUrl = builder.Configuration["GitHub:ApiBaseUrl"] ?? "https://api.github.com",
    UserAgent = builder.Configuration["GitHub:UserAgent"] ?? "ApiAggregation"
};

var openWeatherMapSettings = new OpenWeatherMapSettings
{
    AccessToken = builder.Configuration["OpenWeatherMap:ApiKey"],
    ApiBaseUrl = builder.Configuration["OpenWeatherMap:ApiBaseUrl"] ?? "https://api.openweathermap.org/data/2.5"
};

var newsApiSettings = new NewsApiSettings
{
    AccessToken = builder.Configuration["NewsAPI:ApiKey"],
    ApiBaseUrl = builder.Configuration["NewsAPI:ApiBaseUrl"] ?? "https://newsapi.org/v2"
};

var freeDictionarySettings = new FreeDictionaryApiSettings
{
    ApiBaseUrl = builder.Configuration["FreeDictionary:ApiBaseUrl"] ?? "https://api.dictionaryapi.dev/api/v2/entries/en"
};

var countriesSettings = new CountriesApiSettings
{
    ApiBaseUrl = builder.Configuration["Countries:ApiBaseUrl"] ?? "https://restcountries.com/v3.1"
};

builder.Services.AddHttpClient();

builder.Services.AddSingleton(gitHubSettings);
builder.Services.AddSingleton(openWeatherMapSettings);
builder.Services.AddSingleton(newsApiSettings);
builder.Services.AddSingleton(freeDictionarySettings);
builder.Services.AddSingleton(countriesSettings);

builder.Services.AddSingleton<IApiService, GitHubApiService>();
builder.Services.AddSingleton<IApiService, OpenWeatherMapService>();
builder.Services.AddSingleton<IApiService, NewsApiService>();
builder.Services.AddSingleton<IApiService, SpotifyApiService>();
builder.Services.AddSingleton<IApiService, CountriesApiService>();
builder.Services.AddSingleton<IApiService, FreeDictionaryApiService>();

builder.Services.AddSingleton<SpotifyAuthService>();
builder.Services.AddSingleton<IApiAggregationService, ApiAggregationService>();

var app = builder.Build();

// Configure middleware
app.UseIpRateLimiting();
app.UseResponseCaching();
app.UseResponseCompression();
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowSpecificOrigins");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Add health checks
app.MapHealthChecks("/health");

app.Run();