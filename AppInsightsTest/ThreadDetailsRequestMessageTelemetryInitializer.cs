using System.Threading;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace AppInsightsTest
{
    public class ThreadDetailsRequestMessageTelemetryInitializer : ITelemetryInitializer
    {
        private const string ThreadIdField = "ThreadId";
        private const string ThreadNameField = "ThreadName";
        
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is RequestTelemetry requestTelemetry 
                && requestTelemetry.ItemTypeFlag == SamplingTelemetryItemTypes.Request)
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;
                var threadName = Thread.CurrentThread.Name;
                requestTelemetry.Properties[ThreadIdField] = threadId.ToString();
                requestTelemetry.Properties[ThreadNameField] = threadName;
            }
        }
    }
}