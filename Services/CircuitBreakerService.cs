using Polly;
using Polly.CircuitBreaker;
using System.Net;

namespace ApiAggregation.Services;

public class CircuitBreakerService
{
    private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreaker;
    private readonly ILogger<CircuitBreakerService> _logger;

    public CircuitBreakerService(ILogger<CircuitBreakerService> logger)
    {
        _logger = logger;
        _circuitBreaker = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 2,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, duration) =>
                {
                    _logger.LogWarning("Circuit breaker opened for {Duration}s due to: {Exception}", 
                        duration.TotalSeconds, ex);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker half-open");
                }
            );
    }

    public AsyncCircuitBreakerPolicy<HttpResponseMessage> Policy => _circuitBreaker;
}