namespace CloudStorageORM.Options;

/// <summary>
/// Configures transient-fault retries for provider I/O executed by shared CloudStorageORM infrastructure.
/// </summary>
public class CloudStorageRetryOptions
{
    /// <summary>
    /// Enables retry execution for transient failures. Defaults to disabled for explicit opt-in.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Maximum number of retry attempts after the initial attempt.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay used by exponential backoff.
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Maximum delay cap for exponential backoff.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Jitter factor in the range [0, 1]. Zero disables jitter.
    /// </summary>
    public double JitterFactor { get; set; } = 0.2d;
}