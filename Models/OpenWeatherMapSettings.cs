namespace ApiAggregation.Models
{
    public class OpenWeatherMapSettings : ApiSettings
    {
        public OpenWeatherMapSettings()
        {
            ApiBaseUrl = "https://api.openweathermap.org/data/2.5";
            AdditionalHeaders["Accept"] = "application/json";
        }
    }
} 