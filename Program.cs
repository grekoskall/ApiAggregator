using ApiAggregation.Services;
using ApiAggregation.Models;
using ApiAggregation.Middleware;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreRateLimit;
using Serilog;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
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

// Add Response Caching
builder.Services.AddResponseCaching();

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

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

// ... (rest of the service configurations)

var app = builder.Build();

// Configure middleware
app.UseIpRateLimiting();
app.UseResponseCaching();
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