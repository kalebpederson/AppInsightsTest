using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights;

namespace AppInsightsTest
{
    public class DependencyTrackingRandomNumberProvider : IRandomNumberProvider
    {
        private readonly IRandomNumberProvider _decorated;
        private readonly TelemetryClient _telemetryClient;

        public DependencyTrackingRandomNumberProvider(IRandomNumberProvider decorated, TelemetryClient telemetryClient)
        {
            _decorated = decorated;
            _telemetryClient = telemetryClient;
        }
        
        public int GetRandomNumberUpTo(int max)
        {
            var startTime = DateTime.UtcNow;
            var timer = Stopwatch.StartNew();
            bool success = true;
            try
            {
                return _decorated.GetRandomNumberUpTo(max);
            }
            catch (Exception)
            {
                success = false;
            }
            finally
            {
                timer.Stop();
                _telemetryClient.TrackDependency(
                    nameof(IRandomNumberProvider),
                    nameof(IRandomNumberProvider.GetRandomNumberUpTo),
                    max.ToString(),
                    startTime, timer.Elapsed, success);
            }
            return max-1;
        }
    }
}