using Dmca.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dmca.Tests;

/// <summary>
/// Tests for the DMCA custom exception hierarchy, <see cref="RetryHelper"/>,
/// and <see cref="DmcaLog"/> structured logging helpers.
/// </summary>
public sealed class ErrorHandlingTests
{
    // ═══════════════════════════════════════════════════════════════
    // Exception Hierarchy
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DmcaException_IsBaseForAllCustomExceptions()
    {
        Assert.True(typeof(CollectorException).IsSubclassOf(typeof(DmcaException)));
        Assert.True(typeof(AiClientException).IsSubclassOf(typeof(DmcaException)));
        Assert.True(typeof(ExecutionActionException).IsSubclassOf(typeof(DmcaException)));
        Assert.True(typeof(ApiValidationException).IsSubclassOf(typeof(DmcaException)));
        Assert.True(typeof(SessionStateException).IsSubclassOf(typeof(DmcaException)));
    }

    [Fact]
    public void DmcaException_ExtendsSystemException()
    {
        Assert.True(typeof(DmcaException).IsSubclassOf(typeof(Exception)));
    }

    [Fact]
    public void CollectorException_ContainsCollectorName()
    {
        var ex = new CollectorException("WmiDriver", "Access denied");

        Assert.Equal("WmiDriver", ex.CollectorName);
        Assert.Contains("WmiDriver", ex.Message);
        Assert.Contains("Access denied", ex.Message);
    }

    [Fact]
    public void CollectorException_PreservesInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new CollectorException("PnpUtil", "Timed out", inner);

        Assert.Same(inner, ex.InnerException);
        Assert.Equal("PnpUtil", ex.CollectorName);
    }

    [Fact]
    public void AiClientException_DefaultIsNotTransient()
    {
        var ex = new AiClientException("Error");
        Assert.False(ex.IsTransient);
    }

    [Fact]
    public void AiClientException_TransientFlagSetCorrectly()
    {
        var transient = new AiClientException("Rate limited", isTransient: true);
        var nonTransient = new AiClientException("Bad format", isTransient: false);

        Assert.True(transient.IsTransient);
        Assert.False(nonTransient.IsTransient);
    }

    [Fact]
    public void AiClientException_PreservesInnerException()
    {
        var inner = new HttpRequestException("Network error");
        var ex = new AiClientException("Request failed", inner, isTransient: true);

        Assert.Same(inner, ex.InnerException);
        Assert.True(ex.IsTransient);
    }

    [Fact]
    public void ExecutionActionException_ContainsActionAndTargetIds()
    {
        var ex = new ExecutionActionException("action-123", "drv:test.inf", "Access denied");

        Assert.Equal("action-123", ex.ActionId);
        Assert.Equal("drv:test.inf", ex.TargetId);
        Assert.Contains("action-123", ex.Message);
        Assert.Contains("drv:test.inf", ex.Message);
        Assert.Contains("Access denied", ex.Message);
    }

    [Fact]
    public void ExecutionActionException_PreservesInnerException()
    {
        var inner = new UnauthorizedAccessException("Admin required");
        var ex = new ExecutionActionException("act-1", "svc:Test", "Failed", inner);

        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void ApiValidationException_ContainsMessage()
    {
        var ex = new ApiValidationException("Missing required field: sessionId");
        Assert.Contains("sessionId", ex.Message);
    }

    [Fact]
    public void SessionStateException_ContainsFromAndToStatus()
    {
        var ex = new SessionStateException("NEW", "COMPLETED");

        Assert.Equal("NEW", ex.FromStatus);
        Assert.Equal("COMPLETED", ex.ToStatus);
        Assert.Contains("NEW", ex.Message);
        Assert.Contains("COMPLETED", ex.Message);
        Assert.Contains("Invalid session status transition", ex.Message);
    }

    // ═══════════════════════════════════════════════════════════════
    // RetryHelper
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task RetryHelper_SucceedsOnFirstAttempt()
    {
        var callCount = 0;

        var result = await RetryHelper.ExecuteWithRetryAsync(async () =>
        {
            callCount++;
            await Task.CompletedTask;
            return 42;
        }, maxRetries: 3, baseDelayMs: 1);

        Assert.Equal(42, result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task RetryHelper_RetriesOnFailure_ThenSucceeds()
    {
        var callCount = 0;

        var result = await RetryHelper.ExecuteWithRetryAsync(async () =>
        {
            callCount++;
            if (callCount < 3)
                throw new InvalidOperationException("Transient error");
            await Task.CompletedTask;
            return "success";
        }, maxRetries: 3, baseDelayMs: 1);

        Assert.Equal("success", result);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task RetryHelper_ExhaustsRetries_ThrowsLastException()
    {
        var callCount = 0;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await RetryHelper.ExecuteWithRetryAsync<int>(async () =>
            {
                callCount++;
                await Task.CompletedTask;
                throw new InvalidOperationException($"Failure #{callCount}");
            }, maxRetries: 2, baseDelayMs: 1);
        });

        Assert.Equal(3, callCount); // 1 initial + 2 retries
        Assert.Contains("Failure #", ex.Message);
    }

    [Fact]
    public async Task RetryHelper_ShouldRetryPredicate_FiltersExceptions()
    {
        var callCount = 0;

        var ex = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await RetryHelper.ExecuteWithRetryAsync<int>(async () =>
            {
                callCount++;
                await Task.CompletedTask;
                throw new ArgumentException("Not retryable");
            }, maxRetries: 3, baseDelayMs: 1,
               shouldRetry: e => e is InvalidOperationException);
        });

        Assert.Equal(1, callCount); // No retries for ArgumentException
    }

    [Fact]
    public async Task RetryHelper_VoidOverload_RetriesAndSucceeds()
    {
        var callCount = 0;

        await RetryHelper.ExecuteWithRetryAsync(async () =>
        {
            callCount++;
            if (callCount < 2)
                throw new InvalidOperationException("Transient");
            await Task.CompletedTask;
        }, maxRetries: 3, baseDelayMs: 1);

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task RetryHelper_VoidOverload_ExhaustsRetries_Throws()
    {
        var callCount = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await RetryHelper.ExecuteWithRetryAsync(async () =>
            {
                callCount++;
                await Task.CompletedTask;
                throw new InvalidOperationException("Always fails");
            }, maxRetries: 1, baseDelayMs: 1);
        });

        Assert.Equal(2, callCount); // 1 initial + 1 retry
    }

    [Fact]
    public async Task RetryHelper_CancellationToken_HonoredBetweenRetries()
    {
        using var cts = new CancellationTokenSource();
        var callCount = 0;

        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await RetryHelper.ExecuteWithRetryAsync<int>(async () =>
            {
                callCount++;
                if (callCount == 1)
                    cts.Cancel(); // Cancel after first attempt
                await Task.CompletedTask;
                throw new InvalidOperationException("Transient");
            }, maxRetries: 5, baseDelayMs: 1, ct: cts.Token);
        });

        Assert.Equal(1, callCount);
    }

    // ═══════════════════════════════════════════════════════════════
    // DmcaLog
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DmcaLog_TimedOperation_CompletesWithoutError()
    {
        var logger = NullLogger.Instance;

        using var op = DmcaLog.BeginTimedOperation(logger, "TestOperation");
        // If we get here without exception, the operation was created successfully
        Assert.NotNull(op);
    }

    [Fact]
    public void DmcaLog_TimedOperation_IsDisposable()
    {
        var logger = NullLogger.Instance;
        var op = DmcaLog.BeginTimedOperation(logger, "TestOp");

        Assert.IsAssignableFrom<IDisposable>(op);
        op.Dispose(); // Should not throw
    }

    [Fact]
    public void DmcaLog_EventIds_AreUnique()
    {
        var eventIds = new[]
        {
            DmcaLog.Events.CollectorStarted,
            DmcaLog.Events.CollectorCompleted,
            DmcaLog.Events.CollectorFailed,
            DmcaLog.Events.ScoringStarted,
            DmcaLog.Events.ScoringCompleted,
            DmcaLog.Events.AiRequestSent,
            DmcaLog.Events.AiResponseReceived,
            DmcaLog.Events.AiToolCalled,
            DmcaLog.Events.AiSafetyViolation,
            DmcaLog.Events.ExecutionStarted,
            DmcaLog.Events.ExecutionCompleted,
            DmcaLog.Events.ExecutionFailed,
            DmcaLog.Events.ApiRequestReceived,
            DmcaLog.Events.ApiResponseSent,
        };

        var ids = eventIds.Select(e => e.Id).ToHashSet();
        Assert.Equal(eventIds.Length, ids.Count);
    }

    [Fact]
    public void DmcaLog_EventIds_HaveExpectedRanges()
    {
        // Collector events: 1000-1002
        Assert.Equal(1000, DmcaLog.Events.CollectorStarted.Id);
        Assert.Equal(1001, DmcaLog.Events.CollectorCompleted.Id);
        Assert.Equal(1002, DmcaLog.Events.CollectorFailed.Id);

        // Scoring events: 2000-2001
        Assert.Equal(2000, DmcaLog.Events.ScoringStarted.Id);
        Assert.Equal(2001, DmcaLog.Events.ScoringCompleted.Id);

        // AI events: 3000-3003
        Assert.Equal(3000, DmcaLog.Events.AiRequestSent.Id);
        Assert.Equal(3003, DmcaLog.Events.AiSafetyViolation.Id);

        // Execution events: 4000-4002
        Assert.Equal(4000, DmcaLog.Events.ExecutionStarted.Id);
        Assert.Equal(4002, DmcaLog.Events.ExecutionFailed.Id);

        // API events: 5000-5001
        Assert.Equal(5000, DmcaLog.Events.ApiRequestReceived.Id);
        Assert.Equal(5001, DmcaLog.Events.ApiResponseSent.Id);
    }

    [Fact]
    public void DmcaLog_EventIds_HaveNames()
    {
        Assert.Equal("CollectorStarted", DmcaLog.Events.CollectorStarted.Name);
        Assert.Equal("ExecutionFailed", DmcaLog.Events.ExecutionFailed.Name);
        Assert.Equal("AiSafetyViolation", DmcaLog.Events.AiSafetyViolation.Name);
        Assert.Equal("ApiResponseSent", DmcaLog.Events.ApiResponseSent.Name);
    }
}
