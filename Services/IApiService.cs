using System.Threading.Tasks;
using ApiAggregation.Models;

namespace ApiAggregation.Services
{
    public interface IApiService
    {
        Task<AggregatedData> FetchDataAsync(string endpoint);
        string GetApiName();
    }
} 