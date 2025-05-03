using System;
using System.Net.Http;
using System.Threading.Tasks;
using ApiAggregation.Models;
using System.Text.Json;
using System.Net;
using Polly;
using Polly.Retry;
using System.Threading;

namespace ApiAggregation.Services
{
    public abstract class BaseApiService : IApiService
    {
        protected readonly HttpClient _httpClient;
        protected readonly ApiSettings _settings;
        protected readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        protected BaseApiService(HttpClient httpClient, ApiSettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;

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

        public abstract string GetApiName();

        public virtual async Task<AggregatedData> FetchDataAsync(string endpoint)
        {
            try
            {
                var request = CreateRequest(endpoint);

                var response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    return await _httpClient.SendAsync(request, cts.Token);
                });

                response.EnsureSuccessStatusCode();
                return await ProcessResponse($"{_settings.ApiBaseUrl}{endpoint}", response);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching from {GetApiName()} API: {ex.Message}");
            }
        }

        protected virtual HttpRequestMessage CreateRequest(string endpoint)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.ApiBaseUrl}{endpoint}");
            
            if (!string.IsNullOrEmpty(_settings.AccessToken) && GetApiName() != "OpenWeatherMap")
            {
                request.Headers.Add("Authorization", $"Bearer {_settings.AccessToken}");
            }

            if (!string.IsNullOrEmpty(_settings.UserAgent))
            {
                request.Headers.Add("User-Agent", _settings.UserAgent);
            }

            foreach (var header in _settings.AdditionalHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            return request;
        }

        protected virtual async Task<AggregatedData> ProcessResponse(string apiUrl, HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(content);
                
                return new AggregatedData
                {
                    Id = 0,
                    Source = apiUrl,
                    Data = jsonElement,
                    Timestamp = DateTime.UtcNow,
                    DataType = jsonElement.ValueKind == JsonValueKind.Array ? "Array" : "Object"
                };
            }
            catch (JsonException ex)
            {
                throw new Exception($"Invalid JSON response from {apiUrl}: {ex.Message}");
            }
        }
    }
} 
