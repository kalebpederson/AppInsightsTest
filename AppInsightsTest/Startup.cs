using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;

namespace AppInsightsTest
{
    public class Startup
    {
        private const long OneMegabyte = 1024 * 1024;
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AppInsightsTest", Version = "v1" });
            });
            services.AddMemoryCache();
            services.AddSingleton<IRandomNumberProvider, RandomNumberProvider>();
            services.AddHealthChecks()
                .AddCheck<DependencyAvailableHealthCheck>(
                    "DependencyHealthCheck", HealthStatus.Unhealthy, new [] {"liveness"});

            services.Configure<HealthCheckPublisherOptions>(opts =>
            {
                opts.Delay = TimeSpan.FromSeconds(10);
                opts.Predicate = check => check.Tags.Contains("liveness");
                opts.Period = TimeSpan.FromSeconds(30);
            });
          
            // TODO: Step 2 - Call AddApplicationInsightsTelemetry() extension method to register the
            // appropriate middleware so that hooks will all be in place to enable application monitoring.
            services.AddApplicationInsightsTelemetry(opts =>
            {
                // None of these options are necessary; the defaults are very reasonable.
                opts.EnableAdaptiveSampling = true;                     // enabled by default
                opts.EnableHeartbeat = true;                            // default is not documented
                opts.DeveloperMode = GetAppInsightsDeveloperMode();     // useful to stream logs/events instead of batch
                opts.EnablePerformanceCounterCollectionModule = false;  // enabled by default on Windows
                opts.EnableEventCounterCollectionModule = true;         // enabled by default for NetStandard 2+
            });
            
            // TODO: Step 3 - Add ApplicationInsights section to your application settings (appsettings.json)
            // because we didn't specify an instrumentation key or connection string when enabling the 
            // telemetry as specified above.
            
            // TODO: Step 4 - We can track a dependency that's not already tracked by manually using the
            // telemetry client and adding a custom metric. I prefer a decorator approach for this.
            // We would typically have something like the following:
            //
            // services.AddSingleton<IRandomNumberProvider, RandomNumberProvider>();
            //
            // Instead, we will register a decorated version whose primary responsibility is to track the
            // calls made to the dependency:
            services.AddSingleton<IRandomNumberProvider>(provider =>
                new DependencyTrackingRandomNumberProvider(
                    new RandomNumberProvider(),
                    provider.GetRequiredService<TelemetryClient>()
                    ));
            
            // TODO: Step 6 - Enable a telemetry initializer so we can customize what is being sent as telemetry
            services.AddSingleton<ITelemetryInitializer, ThreadDetailsRequestMessageTelemetryInitializer>();
            
            // TODO: Step 7 - Publish the health check results to Application Insights as availability metrics
            services.AddSingleton<IHealthCheckPublisher, AppInsightsHealthCheckResultsPublisher>();
            
            // TODO: Step 9 - Enable the Application Insights Profiler. This will result in the stack sampler
            // kicking in based on the profiler configuration settings.
            services.AddServiceProfiler(cfg =>
            {
                cfg.IsDisabled = true;
                cfg.CPUTriggerThreshold = 70;
                cfg.Duration = TimeSpan.FromSeconds(30);
            });
            
            // TODO: Step 11 - Enable the Snapshot Debugger so that we have information about exceptions that
            // are thrown within the application. No options are required if the defaults are agreeable.
            // TODO: Step 12 - Add "Application Insights Snapshot Debugger" Role to user(s) that will need
            // to access the Debug Snapshots within Application Insights.
            services.AddSnapshotCollector(cfg =>
            {
                cfg.IsEnabled = true;
                cfg.IsEnabledWhenProfiling = true;
                cfg.IsEnabledInDeveloperMode = true;
                cfg.SnapshotsPerTenMinutesLimit = 10;
            });
            
            // TODO: Step 13 - Secure Live Metrics control plane by using authenticated API calls
            // by:
            // 1. Creating a new API key in the Azure Portal for Application Insights.
            // 2. Configuring the QuickPulseTelemetryModule (live metrics view) to use an API key
            services.ConfigureTelemetryModule<QuickPulseTelemetryModule>((module, opts) =>
            {
                module.AuthenticationApiKey = "pybmwdbwoic4ontzobzfd1knsb0hsy0nukg0bpik";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AppInsightsTest v1"));
            }
            
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = OnException
            });

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthz");
            });
        }

        private Task OnException(HttpContext context)
        {
            var errorContext = context.Features.Get<IExceptionHandlerPathFeature>();
            Log.Logger.Error(
                errorContext.Error,
                "An unhandled exception occurred at {path}: {errorMessage}", 
                errorContext.Path,
                errorContext.Error.Message);
            return Task.CompletedTask;
        }

        private static bool? GetAppInsightsDeveloperMode()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
        
    }
}