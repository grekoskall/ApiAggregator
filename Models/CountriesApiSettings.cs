namespace ApiAggregation.Models
{
    public class CountriesApiSettings : ApiSettings
    {
        public CountriesApiSettings()
        {
            ApiBaseUrl = "https://restcountries.com/v3.1";
            AdditionalHeaders["Accept"] = "application/json"; }
    }
}
