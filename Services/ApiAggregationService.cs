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

        public ApiAggregationService(HttpClient httpClient, GitHubApiSettings gitHubSettings, IEnumerable<IApiService> apiServices)
        {
            _dataStore = new List<AggregatedData>();
            _httpClient = httpClient;
            _fallbackUrls = new Dictionary<string, string>();
            _gitHubSettings = gitHubSettings;
            _apiServices = apiServices.ToDictionary(s => s.GetApiName().ToLower());

            _retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TimeoutException>()
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests || 
                             r.StatusCode == HttpStatusCode.ServiceUnavailable)
                .WaitAndRetryAsync(3, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {exception}");
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
                throw new Exception($"Invalid JSON response from {apiUrl}: {ex.Message}");
            }
        }

        private HttpRequestMessage CreateGitHubRequest(string endpoint)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_gitHubSettings.ApiBaseUrl}{endpoint}");
            request.Headers.Add("Authorization", $"Bearer {_gitHubSettings.AccessToken}");
            request.Headers.Add("User-Agent", _gitHubSettings.UserAgent);
            request.Headers.Add("Accept", "application/vnd.github.v3+json");
            return request;
        }

        public async Task<AggregatedData> FetchFromGitHubAsync(string endpoint)
        {
            try
            {
                var request = CreateGitHubRequest(endpoint);

                var response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    return await _httpClient.SendAsync(request, cts.Token);
                });

                response.EnsureSuccessStatusCode();
                return await ProcessApiResponse($"{_gitHubSettings.ApiBaseUrl}{endpoint}", response);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                throw new Exception("GitHub API authentication failed. Please check your access token.");
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("403"))
            {
                throw new Exception("GitHub API rate limit exceeded or insufficient permissions.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching from GitHub API: {ex.Message}");
            }
        }

        public async Task<AggregatedData> FetchFromExternalApiAsync(string apiUrl)
        {
            try
            {
                var response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    return await _httpClient.GetAsync(apiUrl, cts.Token);
                });

                response.EnsureSuccessStatusCode();
                return await ProcessApiResponse(apiUrl, response);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TimeoutException)
            {
                if (_fallbackUrls.TryGetValue(apiUrl, out string fallbackUrl))
                {
                    try
                    {
                        var fallbackResponse = await _httpClient.GetAsync(fallbackUrl);
                        fallbackResponse.EnsureSuccessStatusCode();
                        return await ProcessApiResponse(fallbackUrl, fallbackResponse);
                    }
                    catch (Exception fallbackEx)
                    {
                        throw new Exception($"Both primary and fallback APIs failed. Primary error: {ex.Message}, Fallback error: {fallbackEx.Message}");
                    }
                }

                throw new Exception($"Failed to fetch data from {apiUrl}: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error while fetching from {apiUrl}: {ex.Message}");
            }
        }

        public async Task<AggregatedData> FetchFromApiAsync(string apiName, string endpoint)
        {
            if (!_apiServices.TryGetValue(apiName.ToLower(), out var apiService))
            {
                throw new Exception($"API service '{apiName}' not found. Available services: {string.Join(", ", _apiServices.Keys)}");
            }

            var data = await apiService.FetchDataAsync(endpoint);
            data.Id = _dataStore.Count + 1;
            _dataStore.Add(data);
            return data;
        }
    }

    public class SpotifyApiSettings : ApiSettings
    {
        public SpotifyApiSettings()
        {
            ApiBaseUrl = "https://api.spotify.com/v1";
            AdditionalHeaders["Accept"] = "application/json";
        }
    }
} 
