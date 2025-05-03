namespace ApiAggregation.Models
{
    public class ApiSettings
    {
        public string ApiBaseUrl { get; set; }
        public string AccessToken { get; set; }
        public string UserAgent { get; set; }
        public Dictionary<string, string> AdditionalHeaders { get; set; } = new();
    }
} 
