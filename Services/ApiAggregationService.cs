using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ApiAggregation.Models;
using System.Text.Json;
using System.Net;
using Polly;
using Polly.Retry;
using System.Threading;
using System.Linq;

namespace ApiAggregation.Services
{
    public class ApiAggregationService : IApiAggregationService
    {
        private readonly List<AggregatedData> _dataStore;
        private readonly HttpClient _httpClient;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly Dictionary<string, string> _fallbackUrls;
        private readonly GitHubApiSettings _gitHubSettings;
        private readonly Dictionary<string, IApiService> _apiServices;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ApiAggregationService> _logger;

        public ApiAggregationService(
            HttpClient httpClient, 
            GitHubApiSettings gitHubSettings, 
            IEnumerable<IApiService> apiServices,
            ICacheService cacheService,
            ILogger<ApiAggregationService> logger)
        {
            _dataStore = new List<AggregatedData>();
            _httpClient = httpClient;
            _fallbackUrls = new Dictionary<string, string>();
            _gitHubSettings = gitHubSettings;
            _apiServices = apiServices.ToDictionary(s => s.GetApiName().ToLower());
            _cacheService = cacheService;
            _logger = logger;

            _retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TimeoutException>()
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests || 
                             r.StatusCode == HttpStatusCode.ServiceUnavailable)
                .WaitAndRetryAsync(3, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Retry {RetryCount} after {TimeSpan}s due to: {Exception}",
                            retryCount, timeSpan.TotalSeconds, exception);
                    });
        }

        public async Task<IEnumerable<AggregatedData>> GetAggregatedDataAsync()
        {
            return await Task.FromResult(_dataStore);
        }

        public async Task<AggregatedData> GetAggregatedDataByIdAsync(int id)
        {
            return await Task.FromResult(_dataStore.Find(x => x.Id == id));
        }

        public async Task<AggregatedData> AddAggregatedDataAsync(AggregatedData data)
        {
            data.Id = _dataStore.Count + 1;
            data.Timestamp = DateTime.UtcNow;
            _dataStore.Add(data);
            return await Task.FromResult(data);
        }

        public async Task<AggregatedData> FetchFromApiAsync(string apiName, string endpoint)
        {
            var cacheKey = $"{apiName.ToLower()}:{endpoint}";
            
            // Try to get from cache
            var cachedData = await _cacheService.GetAsync<AggregatedData>(cacheKey);
            if (cachedData != null)
            {
                _logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
                return cachedData;
            }

            if (!_apiServices.TryGetValue(apiName.ToLower(), out var apiService))
            {
                throw new Exception($"API service '{apiName}' not found. Available services: {string.Join(", ", _apiServices.Keys)}");
            }

            var data = await apiService.FetchDataAsync(endpoint);
            data.Id = _dataStore.Count + 1;
            _dataStore.Add(data);

            // Cache the result
            await _cacheService.SetAsync(cacheKey, data, TimeSpan.FromMinutes(5));
            _logger.LogInformation("Cached data for {CacheKey}", cacheKey);

            return data;
        }

        public void AddFallbackUrl(string primaryUrl, string fallbackUrl)
        {
            _fallbackUrls[primaryUrl] = fallbackUrl;
        }

        private async Task<AggregatedData> ProcessApiResponse(string apiUrl, HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(content);
                
                var aggregatedData = new AggregatedData
                {
                    Id = _dataStore.Count + 1,
                    Source = apiUrl,
                    Data = jsonElement,
                    Timestamp = DateTime.UtcNow,
                    DataType = jsonElement.ValueKind == JsonValueKind.Array ? "Array" : "Object"
                };

                _dataStore.Add(aggregatedData);
                return aggregatedData;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON response from {ApiUrl}", apiUrl);
                throw new Exception($"Invalid JSON response from {apiUrl}: {ex.Message}");
            }
        }

        public async Task<AggregatedData> FetchFromExternalApiAsync(string apiUrl)
        {
            try
            {
                var cacheKey = $"external:{apiUrl}";
                
                // Try to get from cache
                var cachedData = await _cacheService.GetAsync<AggregatedData>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogInformation("Cache hit for external API {ApiUrl}", apiUrl);
                    return cachedData;
                }

                var response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    return await _httpClient.GetAsync(apiUrl, cts.Token);
                });

                response.EnsureSuccessStatusCode();
                var data = await ProcessApiResponse(apiUrl, response);

                // Cache the result
                await _cacheService.SetAsync(cacheKey, data, TimeSpan.FromMinutes(5));
                _logger.LogInformation("Cached data for external API {ApiUrl}", apiUrl);

                return data;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TimeoutException)
            {
                _logger.LogError(ex, "Error fetching from external API {ApiUrl}", apiUrl);
                
                if (_fallbackUrls.TryGetValue(apiUrl, out string fallbackUrl))
                {
                    try
                    {
                        _logger.LogInformation("Attempting fallback for {ApiUrl} to {FallbackUrl}", apiUrl, fallbackUrl);
                        var fallbackResponse = await _httpClient.GetAsync(fallbackUrl);
                        fallbackResponse.EnsureSuccessStatusCode();
                        return await ProcessApiResponse(fallbackUrl, fallbackResponse);
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Fallback failed for {ApiUrl}", apiUrl);
                        throw new Exception($"Both primary and fallback APIs failed. Primary error: {ex.Message}, Fallback error: {fallbackEx.Message}");
                    }
                }

                throw new Exception($"Failed to fetch data from {apiUrl}: {ex.Message}");
            }
        }
    }
}