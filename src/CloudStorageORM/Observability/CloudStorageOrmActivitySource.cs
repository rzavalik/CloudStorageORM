using System.Diagnostics;

namespace CloudStorageORM.Observability;

/// <summary>
/// Central ActivitySource for CloudStorageORM distributed tracing.
/// Provides a singleton instance for all tracing operations.
/// </summary>
public static class CloudStorageOrmActivitySource
{
    private static readonly Lazy<ActivitySource> instance = new(CreateActivitySource);

    /// <summary>
    /// Gets the ActivitySource instance for CloudStorageORM.
    /// </summary>
    public static ActivitySource Instance => instance.Value;

    private static ActivitySource CreateActivitySource()
    {
        return new ActivitySource("CloudStorageORM");
    }

    /// <summary>
    /// Gets or creates an Activity with the specified operation name.
    /// </summary>
    /// <param name="operationName">Name of the operation (e.g., "SaveChanges", "Query").</param>
    /// <param name="kind">Activity kind (default: Internal).</param>
    /// <returns>An Activity if enabled, null otherwise.</returns>
    public static Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal)
    {
        return Instance.StartActivity(operationName, kind);
    }
}