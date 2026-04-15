using CloudStorageORM.Observability;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace CloudStorageORM.Tests.Observability;

public class CloudStorageOrmLoggingExtensionsTests
{
    [Fact]
    public void AllMethods_LogExpectedEntries_WhenLoggerIsProvided()
    {
        var logger = new RecordingLogger();
        var exception = new InvalidOperationException("boom");

        logger.LogConfigurationInitialized("Azure", "users");

        logger.LogQueryExecutionStarting("User");
        logger.LogQueryExecutionCompleted("User", 2, 10);
        logger.LogQueryExecutionFailed("User", exception, 11);

        logger.LogSaveChangesStarting(3);
        logger.LogSaveChangesCompleted(3, 12);
        logger.LogSaveChangesFailed(exception, 13);
        logger.LogEntitySaved("User", null, "updated");

        logger.LogTransactionBeginning("tx-1");
        logger.LogTransactionCommitted("tx-1", 14);
        logger.LogTransactionRolledBack("tx-1", 15);

        logger.LogConcurrencyConflict("User", null, "users/1.json");

        logger.LogBlobUploadStarting("users/1.json", 128);
        logger.LogBlobUploadCompleted("users/1.json", 16);
        logger.LogBlobDownloadStarting("users/1.json");
        logger.LogBlobDownloadCompleted("users/1.json", 128, 17);
        logger.LogBlobDeletionStarting("users/1.json");
        logger.LogBlobDeletionCompleted("users/1.json", 18);

        logger.LogValidationStarting("CloudStorageOptions");
        logger.LogValidationCompleted("CloudStorageOptions", 19);
        logger.LogValidationFailed("CloudStorageOptions", "missing field");
        logger.LogValidationFailed("CloudStorageOptions", "missing field", exception);

        logger.Entries.Count.ShouldBe(22);
        logger.Entries.Count(x => x.EventId == CloudStorageOrmEventIds.ValidationFailed).ShouldBe(2);
        logger.Entries.ShouldContain(x =>
            x.EventId == CloudStorageOrmEventIds.QueryExecutionStarting &&
            x.Message.Contains("User", StringComparison.Ordinal));
        logger.Entries.ShouldContain(x =>
            x.EventId == CloudStorageOrmEventIds.EntitySaved &&
            x.Message.Contains("unknown", StringComparison.Ordinal));
        logger.Entries.ShouldContain(x =>
            x.EventId == CloudStorageOrmEventIds.QueryExecutionFailed && x.Exception == exception);
    }

    [Fact]
    public void AllMethods_DoNotThrow_WhenLoggerIsNull()
    {
        ILogger? logger = null;
        var exception = new InvalidOperationException("boom");

        Should.NotThrow(() =>
        {
            logger.LogConfigurationInitialized("Azure", "users");

            logger.LogQueryExecutionStarting("User");
            logger.LogQueryExecutionCompleted("User", 2, 10);
            logger.LogQueryExecutionFailed("User", exception, 11);

            logger.LogSaveChangesStarting(3);
            logger.LogSaveChangesCompleted(3, 12);
            logger.LogSaveChangesFailed(exception, 13);
            logger.LogEntitySaved("User", null, "updated");

            logger.LogTransactionBeginning("tx-1");
            logger.LogTransactionCommitted("tx-1", 14);
            logger.LogTransactionRolledBack("tx-1", 15);

            logger.LogConcurrencyConflict("User", null, "users/1.json");

            logger.LogBlobUploadStarting("users/1.json", 128);
            logger.LogBlobUploadCompleted("users/1.json", 16);
            logger.LogBlobDownloadStarting("users/1.json");
            logger.LogBlobDownloadCompleted("users/1.json", 128, 17);
            logger.LogBlobDeletionStarting("users/1.json");
            logger.LogBlobDeletionCompleted("users/1.json", 18);

            logger.LogValidationStarting("CloudStorageOptions");
            logger.LogValidationCompleted("CloudStorageOptions", 19);
            logger.LogValidationFailed("CloudStorageOptions", "missing field");
            logger.LogValidationFailed("CloudStorageOptions", "missing field", exception);
        });
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(
        // ReSharper disable once NotAccessedPositionalProperty.Local
        LogLevel Level,
        EventId EventId,
        string Message,
        Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}