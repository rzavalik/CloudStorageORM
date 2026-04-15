using System.Text.Json;
using CloudStorageORM.Abstractions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

public class CloudStorageTransactionManagerTests
{
    private readonly CloudStorageTransactionManager _sut = new();

    [Fact]
    public void BeginTransaction_ReturnsCloudStorageTransaction()
    {
        var tx = _sut.BeginTransaction();
        tx.ShouldNotBeNull();
        tx.ShouldBeOfType<CloudStorageDbContextTransaction>();
        _sut.CurrentTransaction.ShouldBeSameAs(tx);
    }

    [Fact]
    public async Task BeginTransactionAsync_ReturnsCloudStorageTransaction()
    {
        var tx = await _sut.BeginTransactionAsync();
        tx.ShouldNotBeNull();
        tx.ShouldBeOfType<CloudStorageDbContextTransaction>();
        _sut.CurrentTransaction.ShouldBeSameAs(tx);
    }

    [Fact]
    public async Task CommitTransaction_CommitsAndClearsCurrentTransaction()
    {
        await _sut.BeginTransactionAsync();
        var committed = false;

        _sut.EnqueueOperation(_ =>
        {
            committed = true;
            return Task.CompletedTask;
        });

        await _sut.CommitTransactionAsync();

        committed.ShouldBeTrue();
        _sut.CurrentTransaction.ShouldBeNull();

        // Prevent state leakage if an assertion fails earlier.
        await _sut.ResetStateAsync();
    }

    [Fact]
    public void RollbackTransaction_DiscardsPendingOperations()
    {
        _sut.BeginTransaction();
        var committed = false;

        _sut.EnqueueOperation(_ =>
        {
            committed = true;
            return Task.CompletedTask;
        });

        _sut.RollbackTransaction();

        committed.ShouldBeFalse();
        _sut.CurrentTransaction.ShouldBeNull();
    }

    [Fact]
    public void BeginTransaction_WhenAlreadyActive_Throws()
    {
        _sut.BeginTransaction();

        var ex = Should.Throw<InvalidOperationException>(() => _sut.BeginTransaction());
        ex.Message.ShouldContain("already active");
    }

    [Fact]
    public async Task CommitTransactionAsync_CommitsAndClearsCurrentTransaction()
    {
        await _sut.BeginTransactionAsync();
        var committed = false;

        _sut.EnqueueOperation(_ =>
        {
            committed = true;
            return Task.CompletedTask;
        });

        await _sut.CommitTransactionAsync();

        committed.ShouldBeTrue();
        _sut.CurrentTransaction.ShouldBeNull();
    }

    [Fact]
    public async Task CommitTransactionAsync_ExecutesPendingOperationsInOrder()
    {
        await _sut.BeginTransactionAsync();
        var executed = new List<int>();

        _sut.EnqueueOperation(_ =>
        {
            executed.Add(1);
            return Task.CompletedTask;
        });
        _sut.EnqueueOperation(_ =>
        {
            executed.Add(2);
            return Task.CompletedTask;
        });

        await _sut.CommitTransactionAsync();

        executed.ShouldBe([1, 2]);
    }

    [Fact]
    public async Task RollbackTransactionAsync_DiscardsPendingOperations()
    {
        await _sut.BeginTransactionAsync();
        var committed = false;

        _sut.EnqueueOperation(_ =>
        {
            committed = true;
            return Task.CompletedTask;
        });

        await _sut.RollbackTransactionAsync();

        committed.ShouldBeFalse();
        _sut.CurrentTransaction.ShouldBeNull();
    }

    [Fact]
    public void ResetState_ClearsCurrentTransaction()
    {
        _sut.BeginTransaction();

        _sut.ResetState();

        _sut.CurrentTransaction.ShouldBeNull();
    }

    [Fact]
    public async Task ResetStateAsync_ClearsCurrentTransaction()
    {
        await _sut.BeginTransactionAsync();

        await _sut.ResetStateAsync();

        _sut.CurrentTransaction.ShouldBeNull();
    }
}

public class CloudStorageDbContextTransactionTests
{
    [Fact]
    public void Dispose_WithoutCommit_RollsBackPendingOperations()
    {
        var manager = new CloudStorageTransactionManager();
        var tx = manager.BeginTransaction();
        var committed = false;

        manager.EnqueueOperation(_ =>
        {
            committed = true;
            return Task.CompletedTask;
        });

        tx.Dispose();

        committed.ShouldBeFalse();
        manager.CurrentTransaction.ShouldBeNull();
    }

    [Fact]
    public void TransactionId_IsGuid()
    {
        var manager = new CloudStorageTransactionManager();
        var tx = manager.BeginTransaction();
        tx.TransactionId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task TransactionId_IsUniquePerTransaction()
    {
        var manager = new CloudStorageTransactionManager();

        var first = await manager.BeginTransactionAsync();
        var firstId = first.TransactionId;
        await first.RollbackAsync();

        var second = await manager.BeginTransactionAsync();
        var secondId = second.TransactionId;

        secondId.ShouldNotBe(firstId);
    }
}

public class CloudStorageTransactionManagerDurabilityTests
{
    [Fact]
    public async Task Commit_PersistsManifestAndEntityPayload()
    {
        var storage = new InMemoryStorageProvider();
        var manager = new CloudStorageTransactionManager(storage);

        var tx = await manager.BeginTransactionAsync();
        const string userPath = "users/tx-stage2.json";
        var user = new TransactionTestEntity { Id = "tx-stage2", Name = "Stage 2" };

        await manager.StageSaveOperationAsync(userPath, user, CancellationToken.None);
        await tx.CommitAsync();

        var persisted = await storage.ReadAsync<TransactionTestEntity>(userPath);
        persisted.ShouldNotBeNull();
        persisted.Id.ShouldBe("tx-stage2");

        var manifestPath = $"__cloudstorageorm/tx/{tx.TransactionId:D}/manifest.json";
        var manifestJson = storage.GetRawJson(manifestPath);
        manifestJson.ShouldContain("\"state\":2");
    }

    [Fact]
    public async Task BeginTransactionAsync_ReplaysCommittedManifestFromPreviousProcess()
    {
        var storage = new InMemoryStorageProvider();
        var txId = Guid.NewGuid();
        const string userPath = "users/recovered.json";

        var manifestPath = $"__cloudstorageorm/tx/{txId:D}/manifest.json";
        var manifestJson = $$"""
                             {
                               "transactionId": "{{txId}}",
                               "state": 1,
                               "createdAtUtc": "{{DateTimeOffset.UtcNow:O}}",
                               "committedAtUtc": "{{DateTimeOffset.UtcNow:O}}",
                               "operations": [
                                 {
                                   "sequence": 0,
                                   "kind": 0,
                                   "path": "{{userPath}}",
                                   "payloadJson": "{\"id\":\"recovered\",\"name\":\"Recovered User\"}"
                                 }
                               ]
                             }
                             """;

        await storage.SaveRawJsonAsync(manifestPath, manifestJson);

        var manager = new CloudStorageTransactionManager(storage);
        await manager.BeginTransactionAsync();

        var recoveredEntity = await storage.ReadAsync<TransactionTestEntity>(userPath);
        recoveredEntity.ShouldNotBeNull();
        recoveredEntity.Id.ShouldBe("recovered");

        var recoveredManifestJson = storage.GetRawJson(manifestPath);
        recoveredManifestJson.ShouldContain("\"state\":2");
    }

    [Fact]
    public async Task TwoTransactions_UseDistinctStoragePaths()
    {
        var storage = new InMemoryStorageProvider();
        var manager = new CloudStorageTransactionManager(storage);

        var tx1 = await manager.BeginTransactionAsync();
        await manager.StageDeleteOperationAsync("users/a.json", CancellationToken.None);
        await tx1.RollbackAsync();

        var tx2 = await manager.BeginTransactionAsync();
        await manager.StageDeleteOperationAsync("users/b.json", CancellationToken.None);
        await tx2.RollbackAsync();

        storage.GetAllKeys().Any(k =>
                k.Contains($"__cloudstorageorm/tx/{tx1.TransactionId:D}/", StringComparison.Ordinal))
            .ShouldBeTrue();
        storage.GetAllKeys().Any(k =>
                k.Contains($"__cloudstorageorm/tx/{tx2.TransactionId:D}/", StringComparison.Ordinal))
            .ShouldBeTrue();
    }

    private sealed class InMemoryStorageProvider : IStorageProvider
    {
        private readonly Lock _sync = new();
        private readonly Dictionary<string, string> _store = new(StringComparer.Ordinal);
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        // ReSharper disable once UnusedMember.Local
        public IEnumerable<string> Keys
        {
            get
            {
                lock (_sync)
                {
                    return _store.Keys.ToArray();
                }
            }
        }

        public Enums.CloudProvider CloudProvider => Enums.CloudProvider.Azure;

        public Task DeleteContainerAsync()
        {
            lock (_sync)
            {
                _store.Clear();
            }

            return Task.CompletedTask;
        }

        public Task CreateContainerIfNotExistsAsync() => Task.CompletedTask;

        public Task SaveAsync<T>(string path, T entity)
        {
            lock (_sync)
            {
                _store[path] = JsonSerializer.Serialize(entity, _jsonOptions);
            }

            return Task.CompletedTask;
        }

        public Task<string?> SaveAsync<T>(string path, T entity, string? ifMatchETag)
        {
            SaveAsync(path, entity);
            return Task.FromResult<string?>("in-memory-etag");
        }

        public Task<T> ReadAsync<T>(string path)
        {
            lock (_sync)
            {
                if (!_store.TryGetValue(path, out var json))
                {
                    return Task.FromResult(default(T)!);
                }

                return Task.FromResult(JsonSerializer.Deserialize<T>(json, _jsonOptions)!);
            }
        }

        public async Task<StorageObject<T>> ReadWithMetadataAsync<T>(string path)
        {
            var value = await ReadAsync<T>(path);
            var exists = !EqualityComparer<T>.Default.Equals(value, default!);
            return new StorageObject<T>(value, exists ? "in-memory-etag" : null, exists);
        }

        public Task DeleteAsync(string path)
        {
            lock (_sync)
            {
                _store.Remove(path);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string path, string? ifMatchETag) => DeleteAsync(path);

        public Task<List<string>> ListAsync(string folderPath)
        {
            lock (_sync)
            {
                var list = _store.Keys
                    .Where(k => k.StartsWith(folderPath, StringComparison.Ordinal))
                    .OrderBy(k => k, StringComparer.Ordinal)
                    .ToList();
                return Task.FromResult(list);
            }
        }

        public Task<StorageListPage> ListPageAsync(
            string folderPath,
            int pageSize,
            string? continuationToken,
            CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

            lock (_sync)
            {
                var ordered = _store.Keys
                    .Where(k => k.StartsWith(folderPath, StringComparison.Ordinal))
                    .OrderBy(k => k, StringComparer.Ordinal)
                    .ToList();

                var start = 0;
                if (!string.IsNullOrWhiteSpace(continuationToken)
                    && int.TryParse(continuationToken, out var parsedStart)
                    && parsedStart >= 0)
                {
                    start = parsedStart;
                }

                var keys = ordered.Skip(start).Take(pageSize).ToList();
                var nextIndex = start + keys.Count;
                var hasMore = nextIndex < ordered.Count;
                return Task.FromResult(new StorageListPage(keys, hasMore ? nextIndex.ToString() : null, hasMore));
            }
        }

        public string SanitizeBlobName(string rawName) => rawName;

        public Task SaveRawJsonAsync(string path, string json)
        {
            lock (_sync)
            {
                _store[path] = json;
            }

            return Task.CompletedTask;
        }

        public string GetRawJson(string path)
        {
            lock (_sync)
            {
                return _store[path];
            }
        }

        public IReadOnlyList<string> GetAllKeys()
        {
            lock (_sync)
            {
                return _store.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
            }
        }
    }

    private sealed class TransactionTestEntity
    {
        // ReSharper disable once PropertyCanBeMadeInitOnly.Local
        public string Id { get; set; } = string.Empty;

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string Name { get; set; } = string.Empty;
    }
}