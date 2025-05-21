using Microsoft.Extensions.Hosting;

namespace ApiAggregation.Services;

public class BackgroundJobService : BackgroundService
{
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IApiAggregationService _aggregationService;

    public BackgroundJobService(
        ILogger<BackgroundJobService> logger,
        ICacheService cacheService,
        IApiAggregationService aggregationService)
    {
        _logger = logger;
        _cacheService = cacheService;
        _aggregationService = aggregationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshCacheAsync();
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background job");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task RefreshCacheAsync()
    {
        _logger.LogInformation("Starting cache refresh");
        // Implement cache refresh logic here
        _logger.LogInformation("Cache refresh completed");
    }
}