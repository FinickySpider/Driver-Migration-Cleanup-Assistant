namespace Dmca.Core;

/// <summary>
/// Base exception for all DMCA domain errors.
/// Provides a consistent hierarchy for error handling throughout the application.
/// </summary>
public class DmcaException : Exception
{
    public DmcaException(string message) : base(message) { }
    public DmcaException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a collector (WMI, registry, pnputil, etc.) fails to gather data.
/// The application can continue with partial results.
/// </summary>
public sealed class CollectorException : DmcaException
{
    public string CollectorName { get; }

    public CollectorException(string collectorName, string message)
        : base($"Collector '{collectorName}' failed: {message}")
    {
        CollectorName = collectorName;
    }

    public CollectorException(string collectorName, string message, Exception innerException)
        : base($"Collector '{collectorName}' failed: {message}", innerException)
    {
        CollectorName = collectorName;
    }
}

/// <summary>
/// Thrown when the AI advisor client encounters an error (rate limit, network, malformed response).
/// </summary>
public sealed class AiClientException : DmcaException
{
    public bool IsTransient { get; }

    public AiClientException(string message, bool isTransient = false)
        : base(message)
    {
        IsTransient = isTransient;
    }

    public AiClientException(string message, Exception innerException, bool isTransient = false)
        : base(message, innerException)
    {
        IsTransient = isTransient;
    }
}

/// <summary>
/// Thrown when an execution action fails (pnputil error, service access denied, etc.).
/// </summary>
public sealed class ExecutionActionException : DmcaException
{
    public string ActionId { get; }
    public string TargetId { get; }

    public ExecutionActionException(string actionId, string targetId, string message)
        : base($"Action '{actionId}' on target '{targetId}' failed: {message}")
    {
        ActionId = actionId;
        TargetId = targetId;
    }

    public ExecutionActionException(string actionId, string targetId, string message, Exception innerException)
        : base($"Action '{actionId}' on target '{targetId}' failed: {message}", innerException)
    {
        ActionId = actionId;
        TargetId = targetId;
    }
}

/// <summary>
/// Thrown when an API request is invalid.
/// </summary>
public sealed class ApiValidationException : DmcaException
{
    public ApiValidationException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a session state transition is invalid.
/// </summary>
public sealed class SessionStateException : DmcaException
{
    public string FromStatus { get; }
    public string ToStatus { get; }

    public SessionStateException(string fromStatus, string toStatus)
        : base($"Invalid session status transition: {fromStatus} → {toStatus}")
    {
        FromStatus = fromStatus;
        ToStatus = toStatus;
    }
}
