using CloudStorageORM.Options;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// Executes operations with bounded transient-fault retries and exponential backoff.
/// </summary>
internal sealed class CloudStorageRetryExecutor
{
    private readonly CloudStorageRetryOptions _options;
    private readonly Func<Exception, bool> _isTransient;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly Func<double> _nextRandom;

    /// <summary>
    /// Creates a retry executor using the supplied retry options.
    /// </summary>
    /// <param name="options">Retry options controlling retry count, backoff, and jitter.</param>
    public CloudStorageRetryExecutor(CloudStorageRetryOptions options)
        : this(options, CloudStorageTransientExceptionClassifier.IsTransient, Task.Delay,
            () => Random.Shared.NextDouble())
    {
    }

    internal CloudStorageRetryExecutor(
        CloudStorageRetryOptions options,
        Func<Exception, bool> isTransient,
        Func<TimeSpan, CancellationToken, Task> delayAsync,
        Func<double> nextRandom)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _isTransient = isTransient ?? throw new ArgumentNullException(nameof(isTransient));
        _delayAsync = delayAsync ?? throw new ArgumentNullException(nameof(delayAsync));
        _nextRandom = nextRandom ?? throw new ArgumentNullException(nameof(nextRandom));
    }

    /// <summary>
    /// Executes an asynchronous operation with retry handling for transient failures.
    /// </summary>
    /// <param name="operation">Operation to invoke.</param>
    /// <param name="cancellationToken">Cancellation token used for the operation and any retry delay.</param>
    public async Task ExecuteAsync(Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await ExecuteAsync<object?>(
            async token =>
            {
                await operation(token).ConfigureAwait(false);
                return null;
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes an asynchronous operation with retry handling for transient failures and a typed result.
    /// </summary>
    /// <typeparam name="TResult">Result type produced by the operation.</typeparam>
    /// <param name="operation">Operation to invoke.</param>
    /// <param name="cancellationToken">Cancellation token used for the operation and any retry delay.</param>
    /// <returns>The operation result.</returns>
    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var retryCount = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ShouldRetry(ex, retryCount, cancellationToken))
            {
                retryCount++;
                var delay = ComputeDelay(retryCount);
                if (delay > TimeSpan.Zero)
                {
                    await _delayAsync(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private bool ShouldRetry(Exception exception, int retryCount, CancellationToken cancellationToken)
    {
        if (!_options.Enabled || retryCount >= _options.MaxRetries)
        {
            return false;
        }

        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        return _isTransient(exception);
    }

    private TimeSpan ComputeDelay(int retryAttempt)
    {
        if (_options.BaseDelay <= TimeSpan.Zero || _options.MaxDelay <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var exponent = Math.Max(0, retryAttempt - 1);
        var uncappedMilliseconds = _options.BaseDelay.TotalMilliseconds * Math.Pow(2d, exponent);
        var cappedMilliseconds = Math.Min(uncappedMilliseconds, _options.MaxDelay.TotalMilliseconds);

        if (_options.JitterFactor <= 0d)
        {
            return TimeSpan.FromMilliseconds(cappedMilliseconds);
        }

        var jitterWindow = cappedMilliseconds * _options.JitterFactor;
        var jitterOffset = ((_nextRandom() * 2d) - 1d) * jitterWindow;
        var jitteredMilliseconds = Math.Max(0d, cappedMilliseconds + jitterOffset);

        return TimeSpan.FromMilliseconds(jitteredMilliseconds);
    }
}