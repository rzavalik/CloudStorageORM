namespace CloudStorageORM.Observability;

/// <summary>
/// Configuration options for CloudStorageORM observability features including logging, tracing, and diagnostics.
/// </summary>
public class CloudStorageOrmObservabilityOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable structured logging for database operations.
    /// Default: <see langword="true" />.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable distributed tracing via ActivitySource.
    /// Default: <see langword="true" />.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable diagnostic events via DiagnosticListener.
    /// Default: <see langword="true" />.
    /// </summary>
    public bool EnableDiagnostics { get; set; } = true;

    /// <summary>
    /// Gets or sets the custom source name for ActivitySource instrumentation.
    /// If not set, defaults to "CloudStorageORM".
    /// </summary>
    public string? ActivitySourceName { get; set; }

    /// <summary>
    /// Gets or sets the custom diagnostic listener name.
    /// If not set, defaults to "CloudStorageORM".
    /// </summary>
    public string? DiagnosticListenerName { get; set; }

    /// <summary>
    /// Gets the effective activity source name.
    /// </summary>
    /// <returns>Custom name if set, otherwise default "CloudStorageORM".</returns>
    public string GetActivitySourceName() => ActivitySourceName ?? "CloudStorageORM";

    /// <summary>
    /// Gets the effective diagnostic listener name.
    /// </summary>
    /// <returns>Custom name if set, otherwise default "CloudStorageORM".</returns>
    public string GetDiagnosticListenerName() => DiagnosticListenerName ?? "CloudStorageORM";
}