using System.Diagnostics.Metrics;
using System.Collections.Concurrent;

namespace ApiAggregation.Services;

public class MetricsService
{
    private readonly Meter _meter;
    private readonly Counter<long> _apiCallsCounter;
    private readonly Histogram<double> _apiLatencyHistogram;
    private readonly ConcurrentDictionary<string, Counter<long>> _errorCounters;

    public MetricsService()
    {
        _meter = new Meter("ApiAggregation");
        _apiCallsCounter = _meter.CreateCounter<long>("api_calls_total");
        _apiLatencyHistogram = _meter.CreateHistogram<double>("api_latency_seconds");
        _errorCounters = new ConcurrentDictionary<string, Counter<long>>();
    }

    public void RecordApiCall(string apiName)
    {
        _apiCallsCounter.Add(1, new KeyValuePair<string, object?>("api", apiName));
    }

    public void RecordLatency(string apiName, TimeSpan duration)
    {
        _apiLatencyHistogram.Record(duration.TotalSeconds, new KeyValuePair<string, object?>("api", apiName));
    }

    public void RecordError(string apiName, string errorType)
    {
        var counter = _errorCounters.GetOrAdd(
            $"api_errors_{errorType.ToLower()}",
            key => _meter.CreateCounter<long>(key)
        );
        counter.Add(1, new KeyValuePair<string, object?>("api", apiName));
    }
}