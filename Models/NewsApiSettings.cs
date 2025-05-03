namespace ApiAggregation.Models
{
    public class NewsApiSettings : ApiSettings
    {
        public NewsApiSettings()
        {
            ApiBaseUrl = "https://newsapi.org/v2";
            AdditionalHeaders["Accept"] = "application/json";
        }
    }
} 