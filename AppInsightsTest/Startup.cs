using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
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