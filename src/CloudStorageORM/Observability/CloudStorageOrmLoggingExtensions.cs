using Microsoft.Extensions.Logging;

namespace CloudStorageORM.Observability;

/// <summary>
/// Extension methods for CloudStorageORM structured logging.
/// </summary>
internal static class CloudStorageOrmLoggingExtensions
{
    extension(ILogger? logger)
    {
        public void LogQueryExecutionStarting(string entityType,
            string? queryDescription = null)
        {
            logger?.Log(
                LogLevel.Debug,
                CloudStorageOrmEventIds.QueryExecutionStarting,
                "Executing query for entity type '{EntityType}'. {Query}",
                entityType,
                queryDescription ?? string.Empty);
        }

        public void LogQueryExecutionCompleted(string entityType,
            int resultCount,
            long elapsedMilliseconds)
        {
            logger?.Log(
                LogLevel.Debug,
                CloudStorageOrmEventIds.QueryExecutionCompleted,
                "Query for '{EntityType}' completed in {Elapsed}ms with {ResultCount} results.",
                entityType,
                elapsedMilliseconds,
                resultCount);
        }

        public void LogQueryExecutionFailed(string entityType,
            Exception exception,
            long elapsedMilliseconds)
        {
            logger?.Log(
                LogLevel.Error,
                CloudStorageOrmEventIds.QueryExecutionFailed,
                exception,
                "Query for '{EntityType}' failed after {Elapsed}ms.",
                entityType,
                elapsedMilliseconds);
        }

        public void LogSaveChangesStarting(int changeCount)
        {
            logger?.Log(
                LogLevel.Debug,
                CloudStorageOrmEventIds.SaveChangesStarting,
                "Saving {ChangeCount} change(s) to object storage.",
                changeCount);
        }

        public void LogSaveChangesCompleted(int savedCount,
            long elapsedMilliseconds)
        {
            logger?.Log(
                LogLevel.Information,
                CloudStorageOrmEventIds.SaveChangesCompleted,
                "Successfully saved {SavedCount} change(s) in {Elapsed}ms.",
                savedCount,
                elapsedMilliseconds);
        }

        public void LogSaveChangesFailed(Exception exception,
            long elapsedMilliseconds)
        {
            logger?.Log(
                LogLevel.Error,
                CloudStorageOrmEventIds.SaveChangesFailed,
                exception,
                "SaveChanges failed after {Elapsed}ms.",
                elapsedMilliseconds);
        }

        public void LogEntitySaved(string entityType,
            string? entityId,
            string operation)
        {
            logger?.Log(
                LogLevel.Debug,
                CloudStorageOrmEventIds.EntitySaved,
                "Entity '{EntityType}' (ID: {EntityId}) was {Operation}.",
                entityType,
                entityId ?? "unknown",
                operation);
        }

        public void LogTransactionBeginning(string transactionId)
        {
            logger?.Log(
                LogLevel.Debug,
                CloudStorageOrmEventIds.TransactionBeginning,
                "Transaction '{TransactionId}' beginning.",
                transactionId);
        }

        public void LogTransactionCommitted(string transactionId,
            long elapsedMilliseconds)
        {
            logger?.Log(
                LogLevel.Information,
                CloudStorageOrmEventIds.TransactionCommitted,
                "Transaction '{TransactionId}' committed in {Elapsed}ms.",
                transactionId,
                elapsedMilliseconds);
        }

        public void LogTransactionRolledBack(string transactionId,
            long elapsedMilliseconds)
        {
            logger?.Log(
                LogLevel.Information,
                CloudStorageOrmEventIds.TransactionRolledBack,
                "Transaction '{TransactionId}' rolled back in {Elapsed}ms.",
                transactionId,
                elapsedMilliseconds);
        }

        public void LogConcurrencyConflict(string entityType,
            string? entityId,
            string path)
        {
            logger?.Log(
                LogLevel.Warning,
                CloudStorageOrmEventIds.ConcurrencyConflict,
                "Concurrency conflict for entity '{EntityType}' (ID: {EntityId}) at path '{Path}'.",
                entityType,
                entityId ?? "unknown",
                path);
        }

        public void LogBlobUploadStarting(string path, long sizeBytes)
        {
            logger?.Log(
                LogLevel.Debug,
                CloudStorageOrmEventIds.BlobUploadStarting,
                "Uploading blob to '{Path}' ({SizeBytes} bytes).",
                path,
                sizeBytes);
        }

        public void LogBlobUploadCompleted(string path, long elapsedMilliseconds)
        {
            logger?.Log(
                LogLevel.Debug,
                CloudStorageOrmEventIds.BlobUploadCompleted,
                "Blob upload to '{Path}' completed in {Elapsed}ms.",
                path,
                elapsedMilliseconds);
        }

        public void LogBlobDownloadStarting(string path)
        {
            logger?.Log(
                LogLevel.Debug,
                CloudStorageOrmEventIds.BlobDownloadStarting,
                "Downloading blob from '{Path}'.",
                path);
        }

        public void LogBlobDownloadCompleted(string path, long sizeBytes,
            long elapsedMilliseconds)
        {
            logger?.Log(
                LogLevel.Debug,
                CloudStorageOrmEventIds.BlobDownloadCompleted,
                "Blob download from '{Path}' completed in {Elapsed}ms ({SizeBytes} bytes).",
                path,
                elapsedMilliseconds,
                sizeBytes);
        }

        public void LogBlobDeletionStarting(string path)
        {
            logger?.Log(
                LogLevel.Debug,
                CloudStorageOrmEventIds.BlobDeletionStarting,
                "Deleting blob at '{Path}'.",
                path);
        }

        public void LogBlobDeletionCompleted(string path, long elapsedMilliseconds)
        {
            logger?.Log(
                LogLevel.Debug,
                CloudStorageOrmEventIds.BlobDeletionCompleted,
                "Blob deletion at '{Path}' completed in {Elapsed}ms.",
                path,
                elapsedMilliseconds);
        }

        public void LogValidationStarting(string validationType)
        {
            logger?.Log(
                LogLevel.Debug,
                CloudStorageOrmEventIds.ValidationStarting,
                "Validation starting for '{ValidationType}'.",
                validationType);
        }

        public void LogValidationCompleted(string validationType, long elapsedMilliseconds)
        {
            logger?.Log(
                LogLevel.Debug,
                CloudStorageOrmEventIds.ValidationCompleted,
                "Validation for '{ValidationType}' completed in {Elapsed}ms.",
                validationType,
                elapsedMilliseconds);
        }

        public void LogValidationFailed(string validationType,
            string reason,
            Exception? exception = null)
        {
            if (exception != null)
            {
                logger?.Log(
                    LogLevel.Error,
                    CloudStorageOrmEventIds.ValidationFailed,
                    exception,
                    "Validation for '{ValidationType}' failed: {Reason}",
                    validationType,
                    reason);
            }
            else
            {
                logger?.Log(
                    LogLevel.Error,
                    CloudStorageOrmEventIds.ValidationFailed,
                    "Validation for '{ValidationType}' failed: {Reason}",
                    validationType,
                    reason);
            }
        }

        public void LogConfigurationInitialized(string provider,
            string containerName)
        {
            logger?.Log(
                LogLevel.Information,
                CloudStorageOrmEventIds.ConfigurationInitialized,
                "CloudStorageORM configured with provider '{Provider}' and container '{Container}'.",
                provider, containerName);
        }
    }
}