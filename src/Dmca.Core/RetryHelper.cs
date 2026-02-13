namespace Dmca.Core;

/// <summary>
/// Simple retry helper with exponential backoff for transient errors.
/// Used by AI client and WMI collectors.
/// </summary>
public static class RetryHelper
{
    /// <summary>
    /// Retries the given async operation with exponential backoff.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="operation">The operation to attempt.</param>
    /// <param name="maxRetries">Maximum retry attempts (default 3).</param>
    /// <param name="baseDelayMs">Base delay between retries in milliseconds (default 500).</param>
    /// <param name="shouldRetry">Predicate to decide if the exception is retryable. Defaults to all exceptions.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries = 3,
        int baseDelayMs = 500,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken ct = default)
    {
        shouldRetry ??= _ => true;
        Exception? lastException = null;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxRetries && shouldRetry(ex))
            {
                lastException = ex;
                var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay, ct);
            }
        }

        throw lastException!;
    }

    /// <summary>
    /// Retries the given async void operation with exponential backoff.
    /// </summary>
    public static async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        int maxRetries = 3,
        int baseDelayMs = 500,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken ct = default)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true;
        }, maxRetries, baseDelayMs, shouldRetry, ct);
    }
}
