using System;
using System.Net.Http;
using ApiAggregation.Models;

namespace ApiAggregation.Services
{
    public class NewsApiService : BaseApiService
    {
        public NewsApiService(HttpClient httpClient, NewsApiSettings settings) 
            : base(httpClient, settings)
        {
        }

        public override string GetApiName() => "NewsAPI";

        public override async Task<AggregatedData> FetchDataAsync(string endpoint)
        {
            try
            {
                // Add API key to the endpoint
                var endpointWithKey = endpoint.Contains("?") 
                    ? $"{endpoint}&apiKey={_settings.AccessToken}" 
                    : $"{endpoint}?apiKey={_settings.AccessToken}";

                // Remove any Authorization header that might have been set by the base class
                _httpClient.DefaultRequestHeaders.Remove("Authorization");

                return await base.FetchDataAsync(endpointWithKey);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                throw new Exception("NewsAPI authentication failed. Please check your API key.");
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                throw new Exception("NewsAPI rate limit exceeded. Please try again later.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching from NewsAPI: {ex.Message}");
            }
        }
    }
} 