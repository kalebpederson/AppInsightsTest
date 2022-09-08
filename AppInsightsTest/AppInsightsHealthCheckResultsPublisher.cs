using System;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AppInsightsTest
{
    public class AppInsightsHealthCheckResultsPublisher : IHealthCheckPublisher
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly string _applicationName;
        private readonly string _hostname;

        public AppInsightsHealthCheckResultsPublisher(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
            _applicationName = GetApplicationName();
            _hostname = GetHostName();
        }

        public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var telemetry = new AvailabilityTelemetry(
                _applicationName,
                DateTimeOffset.UtcNow,
                report.TotalDuration,
                _hostname,
                report.Status == HealthStatus.Healthy || report.Status == HealthStatus.Degraded
            );
            foreach (var (key, value) in report.Entries)
            {
                telemetry.Properties.Add($"{key}:Description", value.Description);
                telemetry.Properties.Add($"{key}:Duration", value.Duration.ToString());
                telemetry.Properties.Add($"{key}:Status", value.Status.ToString());
                telemetry.Properties.Add($"{key}:Tags", string.Join(",", value.Tags));
                if (value.Exception is not null)
                {
                    telemetry.Properties.Add($"{key}:Exception.Message", value.Exception.Message);
                    telemetry.Properties.Add($"{key}:Exception.Source", value.Exception.Source);
                }
            }
            _telemetryClient.TrackAvailability(telemetry);
            return Task.CompletedTask;
        }

        private static string GetHostName()
        {
            try
            {
                return Dns.GetHostName();
            }
            catch (Exception)
            {
                return "<UnknownHostName>";
            }
        }

        private static string GetApplicationName()
        {
            return $"{Assembly.GetEntryAssembly().GetName().Name ?? "<UnknownApplicationName>"}";
        }
    }
}