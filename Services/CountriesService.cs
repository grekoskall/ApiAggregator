using System;
using System.Net.Http;
using ApiAggregation.Models;

namespace ApiAggregation.Services
{
    public class CountriesApiService : BaseApiService
    {
        public CountriesApiService(HttpClient httpClient, CountriesApiSettings settings) 
            : base(httpClient, settings)
        {
        }

        public override string GetApiName() => "Countries";

        public override async Task<AggregatedData> FetchDataAsync(string endpoint)
        {
            try
            {
                return await base.FetchDataAsync(endpoint);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                throw new Exception("Countries API authentication failed. Please check your access token.");
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("403"))
            {
                throw new Exception("Countries API rate limit exceeded or insufficient permissions.");
            }
        }
    }
} 

