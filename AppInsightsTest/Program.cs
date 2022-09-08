using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace AppInsightsTest
{
    public static class Program
    {
        // TODO: Step 0 - Background information:
        // Standard AspNetCore application with health checks and Serilog logging enabled.
        
        // TODO: Step 1 - Install Microsoft.ApplicationInsights.AspNetCore NuGet Package
        // See: https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core 
        
        // TODO: Step 8 - Install Microsoft.ApplicationInsights.Profiler.AspNetCore
        // see: https://github.com/microsoft/ApplicationInsights-Profiler-AspNetCore
            
        // TODO: Step 10 - Install Microsoft.ApplicationInsights.SnapshotCollector
        // see: https://docs.microsoft.com/en-us/azure/azure-monitor/app/snapshot-debugger-vm
            
        public static int Main(string[] args)
        {
            // TODO: Step 1a - Use the updated activity format when tracing activities/requests
            // default: Hierarchical for < .NET-5
            // see: https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activityidformat?view=net-5.0#System_Diagnostics_ActivityIdFormat_W3C
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            
            // TODO: Step 5 - Call LoggerConfiguration.WriteTo.ApplicationInsights with
            // Install NuGet Package: Serilog.Sinks.ApplicationInsights 
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