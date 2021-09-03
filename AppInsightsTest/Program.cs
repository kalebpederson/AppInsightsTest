using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace AppInsightsTest
{
    public class Program
    {
        // TODO: Step 1 - Install Microsoft.ApplicationInsights.AspNetCore NuGet Package
        // See: https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core 
        
        public static int Main(string[] args)
        {
            // TODO: Step 5 - Call LoggerConfiguration.WriteTo.ApplicationInsights with
            // a configured Telemetry Client to have all of the logs recorded to the traces
            // table within Application Insights.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
#if DEBUG
                .WriteTo.Console()
#endif
                .WriteTo.ApplicationInsights(
                    GetConfiguredTelemetryClient(args),
                    TelemetryConverter.Traces,
                    LogEventLevel.Information)
                .CreateLogger();
            try
            {
                Log.Information("Creating and running web host");
                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Web host failed unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static TelemetryClient GetConfiguredTelemetryClient(string[] args)
        {
            return new TelemetryClient(new TelemetryConfiguration
            {
                ConnectionString = GetAppInsightsConnectionString(args) 
            });
        }

        private static string GetAppInsightsConnectionString(string[] args)
        {
            const bool NotOptional = false;
            const bool ReloadOnChange = true;

            try
            {
                var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", NotOptional, ReloadOnChange)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args)
                    .Build();

                var connectionString = config["ApplicationInsights:ConnectionString"]
                                       ?? throw new InvalidOperationException(
                                           "settings must contain a valid non-null ApplicationInsights:ConnectionString but was null.");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "settings must contain a valid non-empty, non-whitespace ApplicationInsights:ConnectionString but was empty or whitespace.");
                }
                return connectionString;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Unable to read ApplicationInsights:ConnectionString from settings.", ex);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}