using System.Collections.Generic;
using System.Threading.Tasks;
using ApiAggregation.Models;

namespace ApiAggregation.Services
{
    public interface IApiAggregationService
    {
        Task<IEnumerable<AggregatedData>> GetAggregatedDataAsync();
        Task<AggregatedData> GetAggregatedDataByIdAsync(int id);
        Task<AggregatedData> AddAggregatedDataAsync(AggregatedData data);
        Task<AggregatedData> FetchFromExternalApiAsync(string apiUrl);
        Task<AggregatedData> FetchFromApiAsync(string apiName, string endpoint);
    }
} 