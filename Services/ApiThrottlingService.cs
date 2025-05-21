using System.Collections.Concurrent;

namespace ApiAggregation.Services;

public class ApiThrottlingService
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _throttles;
    private readonly ILogger<ApiThrottlingService> _logger;

    public ApiThrottlingService(ILogger<ApiThrottlingService> logger)
    {
        _throttles = new ConcurrentDictionary<string, SemaphoreSlim>();
        _logger = logger;
    }

    public async Task<T> ExecuteWithThrottlingAsync<T>(
        string apiName, 
        Func<Task<T>> action, 
        int maxConcurrent = 3,
        int timeoutSeconds = 30)
    {
        var throttle = _throttles.GetOrAdd(apiName, _ => new SemaphoreSlim(maxConcurrent));

        try
        {
            if (!await throttle.WaitAsync(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                throw new TimeoutException($"Request to {apiName} timed out while waiting for throttle");
            }

            try
            {
                return await action();
            }
            finally
            {
                throttle.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing throttled request to {ApiName}", apiName);
            throw;
        }
    }
}