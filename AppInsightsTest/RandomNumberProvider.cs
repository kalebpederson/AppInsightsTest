using System;
using Microsoft.ApplicationInsights;

namespace AppInsightsTest
{
    public class RandomNumberProvider : IRandomNumberProvider
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly Lazy<Metric> _lazyMetric;
        private readonly object _syncLock = new();
        private readonly Random _random = new();

        public RandomNumberProvider(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
            // TODO: Bonus 2 - We can create pre-aggregated metrics (metrics which are aggregated on the client side
            // and then sent to the server once a minute) by retrieving a metric which is then used to track a value.
            // This will not send each record across, but will send the count, min, max, and the sum of the values.
            // Only pre-aggregated metrics should be used for high-throughput scenarios. The results are available
            // in the customMetrics table.
            _lazyMetric = new Lazy<Metric>(() => _telemetryClient.GetMetric("RandomDelayValue"), true);
        }
        
        public int GetRandomNumberUpTo(int max)
        {
            return Convert.ToInt32(
                (GetRandomStepForPercentile(100, max)
                 + GetRandomStepForPercentile(50, max)
                 + GetRandomStepForPercentile(35, max)
                 + GetRandomStepForPercentile(20, max)
                 + GetRandomStepForPercentile(10, max)
                 + GetRandomStepForPercentile(5, max))
                / 220.0);
        }

        private int GetRandomStepForPercentile(int percent, int max)
        {
            var value = -1;
            switch (percent)
            {
                case < 0:
                case > 100:
                    throw new ArgumentOutOfRangeException(nameof(percent), "Percentage must be between 0 and 100");
                case 100:
                {
                    lock (_syncLock)
                    {
                        value = _random.Next(max);
                    }

                    break;
                }
                default:
                    value = Convert.ToInt32((100 - percent) / 200.0) +
                           _random.Next(Convert.ToInt32((percent / 100.0) * max));
                    break;
            }
            
            // TODO: Bonus 3 - and this is how we record the pre-aggregated value for a single-dimension metric. We
            // can also support multi-dimensional metrics of up to 10 dimensions by passing a second argument that
            // identifies the dimension specified when creating the metric. But, to be able to split by the dimension
            // in the Application Insights user interface, you'll need to enable multi-dimensional metrics:
            // see https://docs.microsoft.com/en-us/azure/azure-monitor/app/get-metric#enable-multi-dimensional-metrics
            _lazyMetric.Value.TrackValue(value);
           
            return value;
        }
        
    }
}