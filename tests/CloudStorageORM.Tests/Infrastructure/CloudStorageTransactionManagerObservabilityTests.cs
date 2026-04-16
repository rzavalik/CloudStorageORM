using System.Text.Json;
using CloudStorageORM.Abstractions;
using CloudStorageORM.Enums;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Observability;
using CloudStorageORM.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shouldly;

// ReSharper disable NotAccessedPositionalProperty.Local

namespace CloudStorageORM.Tests.Infrastructure;

public class CloudStorageTransactionManagerObservabilityTests
{
    [Fact]
    public async Task BeginAndCommit_WhenLoggingEnabled_EmitsTransactionEvents()
    {
        var logger = new RecordingLogger<CloudStorageTransactionManager>();
        var manager = new CloudStorageTransactionManager(
            storageProvider: null,
            cloudStorageOptions: new CloudStorageOptions
            {
                Retry = new CloudStorageRetryOptions(),
                Observability = new CloudStorageOrmObservabilityOptions { EnableLogging = true, EnableTracing = false }
            },
            logger: logger);

        await manager.BeginTransactionAsync();
        manager.EnqueueOperation(_ => Task.CompletedTask);
        await manager.CommitTransactionAsync();

        logger.Entries.ShouldContain(x => x.EventId == CloudStorageOrmEventIds.TransactionBeginning);
        logger.Entries.ShouldContain(x => x.EventId == CloudStorageOrmEventIds.TransactionCommitted);
    }

    [Fact]
    public async Task BeginAndRollback_WhenLoggingDisabled_DoesNotEmitTransactionEvents()
    {
        var logger = new RecordingLogger<CloudStorageTransactionManager>();
        var manager = new CloudStorageTransactionManager(
            storageProvider: null,
            cloudStorageOptions: new CloudStorageOptions
            {
                Retry = new CloudStorageRetryOptions(),
                Observability = new CloudStorageOrmObservabilityOptions { EnableLogging = false, EnableTracing = false }
            },
            logger: logger);

        await manager.BeginTransactionAsync();
        await manager.RollbackTransactionAsync();

        logger.Entries.ShouldNotContain(x => x.EventId == CloudStorageOrmEventIds.TransactionBeginning);
        logger.Entries.ShouldNotContain(x => x.EventId == CloudStorageOrmEventIds.TransactionRolledBack);
    }

    [Fact]
    public async Task DurableCommit_WhenConditionalWriteConflicts_LogsConcurrencyConflict()
    {
        var logger = new RecordingLogger<CloudStorageTransactionManager>();
        var storage = new ObservabilityStorageProvider { ForcePreconditionFailureOnConditionalWrite = true };
        var manager = new CloudStorageTransactionManager(
            storage,
            new CloudStorageOptions
            {
                Retry = new CloudStorageRetryOptions(),
                Observability = new CloudStorageOrmObservabilityOptions { EnableLogging = true, EnableTracing = false }
            },
            logger);

        var tx = await manager.BeginTransactionAsync();
        await manager.StageSaveOperationAsync("users/conflict.json", new { Id = "1" }, "etag-old",
            CancellationToken.None);

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => tx.CommitAsync());

        logger.Entries.ShouldContain(x => x.EventId == CloudStorageOrmEventIds.ConcurrencyConflict);
        logger.Entries.ShouldContain(x => x.EventId == CloudStorageOrmEventIds.BlobUploadStarting);
    }

    private sealed class ObservabilityStorageProvider : IStorageProvider
    {
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
        private readonly Dictionary<string, string> _store = new(StringComparer.Ordinal);
        public bool ForcePreconditionFailureOnConditionalWrite { get; set; }

        public CloudProvider CloudProvider => CloudProvider.Azure;

        public Task DeleteContainerAsync()
        {
            _store.Clear();
            return Task.CompletedTask;
        }

        public Task CreateContainerIfNotExistsAsync() => Task.CompletedTask;

        public Task SaveAsync<T>(string path, T entity)
        {
            _store[path] = JsonSerializer.Serialize(entity, _jsonOptions);
            return Task.CompletedTask;
        }

        public Task<string?> SaveAsync<T>(string path, T entity, string? ifMatchETag)
        {
            if (ForcePreconditionFailureOnConditionalWrite && !string.IsNullOrWhiteSpace(ifMatchETag))
            {
                throw new StoragePreconditionFailedException(path);
            }

            _store[path] = JsonSerializer.Serialize(entity, _jsonOptions);
            return Task.FromResult<string?>("etag-new");
        }

        public Task<T> ReadAsync<T>(string path)
        {
            return Task.FromResult(!_store.TryGetValue(path, out var json)
                ? default!
                : JsonSerializer.Deserialize<T>(json, _jsonOptions)!)!;
        }

        public async Task<StorageObject<T>> ReadWithMetadataAsync<T>(string path)
        {
            var value = await ReadAsync<T>(path);
            var exists = !EqualityComparer<T>.Default.Equals(value, default!);
            return new StorageObject<T>(value, exists ? "etag" : null, exists);
        }

        public Task DeleteAsync(string path)
        {
            _store.Remove(path);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string path, string? ifMatchETag)
        {
            if (ForcePreconditionFailureOnConditionalWrite && !string.IsNullOrWhiteSpace(ifMatchETag))
            {
                throw new StoragePreconditionFailedException(path);
            }

            _store.Remove(path);
            return Task.CompletedTask;
        }

        public Task<List<string>> ListAsync(string folderPath)
        {
            var keys = _store.Keys.Where(k => k.StartsWith(folderPath, StringComparison.Ordinal)).ToList();
            return Task.FromResult(keys);
        }

        public Task<StorageListPage> ListPageAsync(string folderPath, int pageSize, string? continuationToken,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StorageListPage([], null, false));
        }

        public string SanitizeBlobName(string rawName) => rawName;
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(eventId, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(
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