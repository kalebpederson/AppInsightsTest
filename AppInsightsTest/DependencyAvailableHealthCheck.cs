using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AppInsightsTest
{
    public class DependencyAvailableHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Random _random = new();

        public DependencyAvailableHealthCheck(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
        {
            try
            {
                var service = GetOptionalServiceOrThrow<IRandomNumberProvider>();
                var delay = _random.Next(8000);
                await Task.Delay(delay, cancellationToken);
                if (service is null)
                {
                    return 
                        HealthCheckResult.Unhealthy(
                            $"Unable to resolve {nameof(IRandomNumberProvider)} from IoC container.");
                }
            }
            catch (Exception ex)
            {
                return 
                    HealthCheckResult.Unhealthy(
                        $"Exception thrown while attempting to resolve {nameof(IRandomNumberProvider)}",
                        ex);
            }
            return HealthCheckResult.Healthy();
        }

        private T GetOptionalServiceOrThrow<T>()
        {
            return _serviceProvider.GetService<T>();
        }
    }
}