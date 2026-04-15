using Microsoft.Extensions.Logging;

namespace CloudStorageORM.Observability;

/// <summary>
/// Event IDs for CloudStorageORM logging and diagnostics.
/// </summary>
public static class CloudStorageOrmEventIds
{
    // Configuration
    /// <summary>Event ID for configuration initialized.</summary>
    public static readonly EventId ConfigurationInitialized = new(1001, nameof(ConfigurationInitialized));

    // Query operations
    /// <summary>Event ID for query execution starting.</summary>
    public static readonly EventId QueryExecutionStarting = new(2001, nameof(QueryExecutionStarting));

    /// <summary>Event ID for query execution completed.</summary>
    public static readonly EventId QueryExecutionCompleted = new(2002, nameof(QueryExecutionCompleted));

    /// <summary>Event ID for query execution failed.</summary>
    public static readonly EventId QueryExecutionFailed = new(2003, nameof(QueryExecutionFailed));

    // Save operations
    /// <summary>Event ID for SaveChanges starting.</summary>
    public static readonly EventId SaveChangesStarting = new(3001, nameof(SaveChangesStarting));

    /// <summary>Event ID for SaveChanges completed.</summary>
    public static readonly EventId SaveChangesCompleted = new(3002, nameof(SaveChangesCompleted));

    /// <summary>Event ID for SaveChanges failed.</summary>
    public static readonly EventId SaveChangesFailed = new(3003, nameof(SaveChangesFailed));

    /// <summary>Event ID for entity being saved.</summary>
    public static readonly EventId EntitySaved = new(3004, nameof(EntitySaved));

    // Transaction operations
    /// <summary>Event ID for transaction beginning.</summary>
    public static readonly EventId TransactionBeginning = new(4001, nameof(TransactionBeginning));

    /// <summary>Event ID for transaction committed.</summary>
    public static readonly EventId TransactionCommitted = new(4002, nameof(TransactionCommitted));

    /// <summary>Event ID for transaction rolled back.</summary>
    public static readonly EventId TransactionRolledBack = new(4003, nameof(TransactionRolledBack));

    // Concurrency
    /// <summary>Event ID for concurrency conflict detected.</summary>
    public static readonly EventId ConcurrencyConflict = new(5001, nameof(ConcurrencyConflict));

    // Provider operations
    /// <summary>Event ID for blob upload starting.</summary>
    public static readonly EventId BlobUploadStarting = new(6001, nameof(BlobUploadStarting));

    /// <summary>Event ID for blob upload completed.</summary>
    public static readonly EventId BlobUploadCompleted = new(6002, nameof(BlobUploadCompleted));

    /// <summary>Event ID for blob download starting.</summary>
    public static readonly EventId BlobDownloadStarting = new(6003, nameof(BlobDownloadStarting));

    /// <summary>Event ID for blob download completed.</summary>
    public static readonly EventId BlobDownloadCompleted = new(6004, nameof(BlobDownloadCompleted));

    /// <summary>Event ID for blob deletion starting.</summary>
    public static readonly EventId BlobDeletionStarting = new(6005, nameof(BlobDeletionStarting));

    /// <summary>Event ID for blob deletion completed.</summary>
    public static readonly EventId BlobDeletionCompleted = new(6006, nameof(BlobDeletionCompleted));

    // Validation
    /// <summary>Event ID for validation starting.</summary>
    public static readonly EventId ValidationStarting = new(7001, nameof(ValidationStarting));

    /// <summary>Event ID for validation completed.</summary>
    public static readonly EventId ValidationCompleted = new(7002, nameof(ValidationCompleted));

    /// <summary>Event ID for validation failed.</summary>
    public static readonly EventId ValidationFailed = new(7003, nameof(ValidationFailed));
}