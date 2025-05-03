using System;
using System.Net.Http;
using ApiAggregation.Models;

namespace ApiAggregation.Services
{
    public class OpenWeatherMapService : BaseApiService
    {
        public OpenWeatherMapService(HttpClient httpClient, OpenWeatherMapSettings settings) 
            : base(httpClient, settings)
        {
        }

        public override string GetApiName() => "OpenWeatherMap";

        public override async Task<AggregatedData> FetchDataAsync(string endpoint)
        {
            try
            {
                var endpointWithKey = endpoint.Contains("?") 
                    ? $"{endpoint}&appid={_settings.AccessToken}" 
                    : $"{endpoint}?appid={_settings.AccessToken}";

                _httpClient.DefaultRequestHeaders.Remove("Authorization");

                return await base.FetchDataAsync(endpointWithKey);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                throw new Exception("OpenWeatherMap API authentication failed. Please check your API key.");
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("404"))
            {
                throw new Exception("Location not found. Please check the city name or coordinates.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching from OpenWeatherMap API: {ex.Message}");
            }
        }
    }
} 
