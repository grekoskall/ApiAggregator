using System;
using System.Net.Http;
using ApiAggregation.Models;

namespace ApiAggregation.Services
{
    public class GitHubApiService : BaseApiService
    {
        public GitHubApiService(HttpClient httpClient, GitHubApiSettings settings) 
            : base(httpClient, settings)
        {
        }

        public override string GetApiName() => "GitHub";

        public override async Task<AggregatedData> FetchDataAsync(string endpoint)
        {
            try
            {
                return await base.FetchDataAsync(endpoint);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                throw new Exception("GitHub API authentication failed. Please check your access token.");
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("403"))
            {
                throw new Exception("GitHub API rate limit exceeded or insufficient permissions.");
            }
        }
    }
} 