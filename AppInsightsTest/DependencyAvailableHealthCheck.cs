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

        public DependencyAvailableHealthCheck(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
        {
            try
            {
                var service = GetOptionalServiceOrThrow<IRandomNumberProvider>();
                if (service is null)
                {
                    return Task.FromResult(
                        HealthCheckResult.Unhealthy(
                            $"Unable to resolve {nameof(IRandomNumberProvider)} from IoC container."));
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy(
                        $"Exception thrown while attempting to resolve {nameof(IRandomNumberProvider)}",
                        ex));
            }
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        private T GetOptionalServiceOrThrow<T>()
        {
            return _serviceProvider.GetService<T>();
        }
    }
}