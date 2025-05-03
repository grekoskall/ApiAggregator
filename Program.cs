using ApiAggregation.Services;
using ApiAggregation.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
