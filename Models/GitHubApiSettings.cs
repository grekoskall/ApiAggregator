namespace ApiAggregation.Models
{
    public class GitHubApiSettings : ApiSettings
    {
        public GitHubApiSettings()
        {
            ApiBaseUrl = "https://api.github.com";
            UserAgent = "ApiAggregation";
            AdditionalHeaders["Accept"] = "application/vnd.github.v3+json";
        }
    }
} 