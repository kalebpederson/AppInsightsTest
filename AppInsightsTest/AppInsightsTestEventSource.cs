// Copyright © 2002-2022 HealthEquity, Inc.

using System.Diagnostics.Tracing;

namespace AppInsightsTest;

[EventSource(Name = "HealthEquity-AppInsightsTest-Core")]
internal sealed class AppInsightsTestEventSource : EventSource
{
    public static readonly AppInsightsTestEventSource Log = new();

    /// <summary>
    /// Allows the caller to determine whether verbose logging is enabled when such an operation would have a high
    /// cost when unnecessary.
    /// </summary>
    public static bool IsVerboseEnabled
    {
        [NonEvent]
        get => Log.IsEnabled(EventLevel.Verbose, (EventKeywords)(-1));
    }

    /// <summary>
    /// An operation is being started.
    /// </summary>
    /// <param name="opName">The name of the operation being started.</param>
    [Event(1, Keywords = Keywords.Diagnostics, Message = "Operation {0} started.", Level = EventLevel.Verbose)]
    public void OperationStarted(string opName) => WriteEvent(1, opName ?? string.Empty);

    /// <summary>
    /// An operation has ended.
    /// </summary>
    /// <param name="opName">The name of the completed operation.</param>
    [Event(2, Keywords = Keywords.Diagnostics, Message = "Operation {0} ended.", Level = EventLevel.Verbose)]
    public void OperationEnded(string opName) => WriteEvent(2, opName ?? string.Empty);
 
    /// <summary>
    /// Enables <c>Keyword</c> lookup.
    /// </summary>
    /// <remarks>
    /// This class *MUST* be an inner class within the Event Source for it to be properly picked up. No compile
    /// time error will result even though it will not function properly behind the scenes.
    /// </remarks>
    public static class Keywords
    {
        public const EventKeywords Diagnostics = (EventKeywords)EventSourceKeywords.Diagnostics;
    }
}