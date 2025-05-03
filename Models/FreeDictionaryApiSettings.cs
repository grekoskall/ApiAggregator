namespace ApiAggregation.Models
{
    public class FreeDictionaryApiSettings : ApiSettings
    {
        public FreeDictionaryApiSettings()
        {
            ApiBaseUrl = "https://api.dictionaryapi.dev/api/v2/entries/en";
            AdditionalHeaders["Accept"] = "application/json";
        }
    }
}
