using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Dmca.Core;

/// <summary>
/// Structured logging helpers for DMCA. Provides consistent log formatting
/// and timing instrumentation across all subsystems.
/// </summary>
public static class DmcaLog
{
    /// <summary>
    /// Creates a timed scope that logs duration on disposal.
    /// Usage: using var _ = DmcaLog.BeginTimedOperation(logger, "ScanService.ScanAsync");
    /// </summary>
    public static TimedOperation BeginTimedOperation(ILogger logger, string operationName)
    {
        return new TimedOperation(logger, operationName);
    }

    /// <summary>
    /// A disposable timing scope that logs start and elapsed time.
    /// </summary>
    public sealed class TimedOperation : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;

        internal TimedOperation(ILogger logger, string operationName)
        {
            _logger = logger;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("[{Operation}] Starting...", _operationName);
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _logger.LogInformation("[{Operation}] Completed in {ElapsedMs}ms",
                _operationName, _stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Standard log event IDs for consistent structured logging.
    /// </summary>
    public static class Events
    {
        public static readonly EventId CollectorStarted = new(1000, "CollectorStarted");
        public static readonly EventId CollectorCompleted = new(1001, "CollectorCompleted");
        public static readonly EventId CollectorFailed = new(1002, "CollectorFailed");

        public static readonly EventId ScoringStarted = new(2000, "ScoringStarted");
        public static readonly EventId ScoringCompleted = new(2001, "ScoringCompleted");

        public static readonly EventId AiRequestSent = new(3000, "AiRequestSent");
        public static readonly EventId AiResponseReceived = new(3001, "AiResponseReceived");
        public static readonly EventId AiToolCalled = new(3002, "AiToolCalled");
        public static readonly EventId AiSafetyViolation = new(3003, "AiSafetyViolation");

        public static readonly EventId ExecutionStarted = new(4000, "ExecutionStarted");
        public static readonly EventId ExecutionCompleted = new(4001, "ExecutionCompleted");
        public static readonly EventId ExecutionFailed = new(4002, "ExecutionFailed");

        public static readonly EventId ApiRequestReceived = new(5000, "ApiRequestReceived");
        public static readonly EventId ApiResponseSent = new(5001, "ApiResponseSent");
    }
}
