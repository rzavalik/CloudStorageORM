using System.Text.Json;
using CloudStorageORM.Abstractions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore;
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

        await manager.StageSaveOperationAsync(userPath, user, null, CancellationToken.None);
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
        var manifestJson =
            $$"""
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
        await manager.StageDeleteOperationAsync("users/a.json", null, CancellationToken.None);
        await tx1.RollbackAsync();

        var tx2 = await manager.BeginTransactionAsync();
        await manager.StageDeleteOperationAsync("users/b.json", null, CancellationToken.None);
        await tx2.RollbackAsync();

        storage.GetAllKeys().Any(k =>
                k.Contains($"__cloudstorageorm/tx/{tx1.TransactionId:D}/", StringComparison.Ordinal))
            .ShouldBeTrue();
        storage.GetAllKeys().Any(k =>
                k.Contains($"__cloudstorageorm/tx/{tx2.TransactionId:D}/", StringComparison.Ordinal))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Commit_WithInterruptedReplay_ResumesFromLastAppliedOperation()
    {
        var storage = new InMemoryStorageProvider();
        var txId = Guid.NewGuid();
        const string userPath1 = "users/resume-1.json";
        const string userPath2 = "users/resume-2.json";

        // Simulate a manifest that was in the middle of applying operations:
        // Operation 0 was applied, but operation 1 was not.
        var manifestPath = $"__cloudstorageorm/tx/{txId:D}/manifest.json";
        var manifestJson =
            $$"""
              {
                "transactionId": "{{txId}}",
                "state": 1,
                "createdAtUtc": "{{DateTimeOffset.UtcNow:O}}",
                "committedAtUtc": "{{DateTimeOffset.UtcNow:O}}",
                "appliedOperationCount": 1,
                "operations": [
                  {
                    "sequence": 0,
                    "kind": 0,
                    "path": "{{userPath1}}",
                    "payloadJson": "{\"id\":\"resume-1\",\"name\":\"First\"}"
                  },
                  {
                    "sequence": 1,
                    "kind": 0,
                    "path": "{{userPath2}}",
                    "payloadJson": "{\"id\":\"resume-2\",\"name\":\"Second\"}"
                  }
                ]
              }
              """;

        await storage.SaveRawJsonAsync(manifestPath, manifestJson);
        await storage.SaveAsync(userPath1, new TransactionTestEntity { Id = "resume-1", Name = "First" });

        var manager = new CloudStorageTransactionManager(storage);
        await manager.BeginTransactionAsync();

        // After recovery, the second entity should be applied but not the first (already applied).
        var secondEntity = await storage.ReadAsync<TransactionTestEntity>(userPath2);
        secondEntity.ShouldNotBeNull();
        secondEntity.Id.ShouldBe("resume-2");

        var recoveredManifestJson = storage.GetRawJson(manifestPath);
        recoveredManifestJson.ShouldContain("\"state\":2");
        recoveredManifestJson.ShouldContain("\"appliedOperationCount\":2");
    }

    [Fact]
    public async Task Recovery_IsIdempotent_ReRunningRecoveryDoesNotDuplicateSideEffects()
    {
        var storage = new InMemoryStorageProvider();
        var txId = Guid.NewGuid();
        const string userPath = "users/idempotent.json";

        var manifestPath = $"__cloudstorageorm/tx/{txId:D}/manifest.json";
        var manifestJson =
            $$"""
              {
                "transactionId": "{{txId}}",
                "state": 1,
                "createdAtUtc": "{{DateTimeOffset.UtcNow:O}}",
                "committedAtUtc": "{{DateTimeOffset.UtcNow:O}}",
                "appliedOperationCount": 0,
                "operations": [
                  {
                    "sequence": 0,
                    "kind": 0,
                    "path": "{{userPath}}",
                    "payloadJson": "{\"id\":\"idempotent\",\"name\":\"Test\"}"
                  }
                ]
              }
              """;

        await storage.SaveRawJsonAsync(manifestPath, manifestJson);

        var manager1 = new CloudStorageTransactionManager(storage);
        await manager1.BeginTransactionAsync();

        var entityCount1 = storage.SaveRequests.Count(x => x.Path == userPath);
        entityCount1.ShouldBe(1); // First recovery applies the operation

        // Verify the manifest is now completed with full progress.
        var recoveredManifestJson = storage.GetRawJson(manifestPath);
        recoveredManifestJson.ShouldContain("\"state\":2"); // Completed state
        recoveredManifestJson.ShouldContain("\"appliedOperationCount\":1"); // All operations applied

        // Run recovery again (simulating a new manager instance on already-completed manifest).
        var manager2 = new CloudStorageTransactionManager(storage);
        await manager2.BeginTransactionAsync();

        var entityCount2 = storage.SaveRequests.Count(x => x.Path == userPath);
        entityCount2.ShouldBe(1); // No re-application on second recovery (manifest is Completed, not Committed)
    }

    [Fact]
    public async Task Recovery_WithFullyAppliedManifest_TransitionsDirectlyToCompleted()
    {
        var storage = new InMemoryStorageProvider();
        var txId = Guid.NewGuid();
        const string userPath = "users/fully-applied.json";

        var manifestPath = $"__cloudstorageorm/tx/{txId:D}/manifest.json";
        var manifestJson =
            $$"""
              {
                "transactionId": "{{txId}}",
                "state": 1,
                "createdAtUtc": "{{DateTimeOffset.UtcNow:O}}",
                "committedAtUtc": "{{DateTimeOffset.UtcNow:O}}",
                "appliedOperationCount": 1,
                "operations": [
                  {
                    "sequence": 0,
                    "kind": 0,
                    "path": "{{userPath}}",
                    "payloadJson": "{\"id\":\"fully\",\"name\":\"Applied\"}"
                  }
                ]
              }
              """;

        await storage.SaveRawJsonAsync(manifestPath, manifestJson);
        await storage.SaveAsync(userPath, new TransactionTestEntity { Id = "fully", Name = "Applied" });

        var manager = new CloudStorageTransactionManager(storage);
        await manager.BeginTransactionAsync();

        var recoveredManifestJson = storage.GetRawJson(manifestPath);
        recoveredManifestJson.ShouldContain("\"state\":2"); // Completed
        recoveredManifestJson.ShouldContain("\"appliedOperationCount\":1");
    }

    [Fact]
    public async Task Recovery_WithMultipleInterruptedOperations_ResumesAndCompletesAll()
    {
        var storage = new InMemoryStorageProvider();
        var txId = Guid.NewGuid();
        const string path1 = "items/item1.json";
        const string path2 = "items/item2.json";
        const string path3 = "items/item3.json";

        // Three operations, only the first one was applied.
        var manifestPath = $"__cloudstorageorm/tx/{txId:D}/manifest.json";
        var manifestJson =
            $$"""
              {
                "transactionId": "{{txId}}",
                "state": 1,
                "createdAtUtc": "{{DateTimeOffset.UtcNow:O}}",
                "committedAtUtc": "{{DateTimeOffset.UtcNow:O}}",
                "appliedOperationCount": 1,
                "operations": [
                  {
                    "sequence": 0,
                    "kind": 0,
                    "path": "{{path1}}",
                    "payloadJson": "{\"id\":\"1\",\"name\":\"One\"}"
                  },
                  {
                    "sequence": 1,
                    "kind": 0,
                    "path": "{{path2}}",
                    "payloadJson": "{\"id\":\"2\",\"name\":\"Two\"}"
                  },
                  {
                    "sequence": 2,
                    "kind": 0,
                    "path": "{{path3}}",
                    "payloadJson": "{\"id\":\"3\",\"name\":\"Three\"}"
                  }
                ]
              }
              """;

        await storage.SaveRawJsonAsync(manifestPath, manifestJson);
        await storage.SaveAsync(path1, new TransactionTestEntity { Id = "1", Name = "One" });

        var manager = new CloudStorageTransactionManager(storage);
        await manager.BeginTransactionAsync();

        // All three entities should exist after recovery.
        (await storage.ReadAsync<TransactionTestEntity>(path1)).ShouldNotBeNull();
        (await storage.ReadAsync<TransactionTestEntity>(path2)).ShouldNotBeNull();
        (await storage.ReadAsync<TransactionTestEntity>(path3)).ShouldNotBeNull();

        var recoveredManifestJson = storage.GetRawJson(manifestPath);
        recoveredManifestJson.ShouldContain("\"appliedOperationCount\":3");
    }

    [Fact]
    public async Task Recovery_WithMixedSaveAndDeleteOperations_ResumesCorrectly()
    {
        var storage = new InMemoryStorageProvider();
        var txId = Guid.NewGuid();
        const string existingPath = "data/existing.json";
        const string newPath = "data/new.json";
        const string deletePath = "data/delete-me.json";

        // Pre-create an entity to delete.
        await storage.SaveAsync(deletePath, new TransactionTestEntity { Id = "to-delete", Name = "ToDelete" });

        // Manifest with mixed operations: save, delete (partial progress).
        var manifestPath = $"__cloudstorageorm/tx/{txId:D}/manifest.json";
        var manifestJson =
            $$"""
              {
                "transactionId": "{{txId}}",
                "state": 1,
                "createdAtUtc": "{{DateTimeOffset.UtcNow:O}}",
                "committedAtUtc": "{{DateTimeOffset.UtcNow:O}}",
                "appliedOperationCount": 1,
                "operations": [
                  {
                    "sequence": 0,
                    "kind": 0,
                    "path": "{{existingPath}}",
                    "payloadJson": "{\"id\":\"existing\",\"name\":\"Existing\"}"
                  },
                  {
                    "sequence": 1,
                    "kind": 1,
                    "path": "{{deletePath}}",
                    "payloadJson": null
                  },
                  {
                    "sequence": 2,
                    "kind": 0,
                    "path": "{{newPath}}",
                    "payloadJson": "{\"id\":\"new\",\"name\":\"New\"}"
                  }
                ]
              }
              """;

        await storage.SaveRawJsonAsync(manifestPath, manifestJson);
        await storage.SaveAsync(existingPath, new TransactionTestEntity { Id = "existing", Name = "Existing" });

        var manager = new CloudStorageTransactionManager(storage);
        await manager.BeginTransactionAsync();

        // Verify operations were applied correctly.
        (await storage.ReadAsync<TransactionTestEntity>(existingPath)).ShouldNotBeNull();
        (await storage.ReadAsync<TransactionTestEntity>(deletePath)).ShouldBeNull(); // Deleted
        (await storage.ReadAsync<TransactionTestEntity>(newPath)).ShouldNotBeNull(); // Created

        var recoveredManifestJson = storage.GetRawJson(manifestPath);
        recoveredManifestJson.ShouldContain("\"appliedOperationCount\":3");
    }

    [Fact]
    public async Task Commit_ProgressMarkerIsPersistedAfterEachOperation()
    {
        var storage = new InMemoryStorageProvider();
        var manager = new CloudStorageTransactionManager(storage);

        var tx = await manager.BeginTransactionAsync();
        const string path1 = "users/p1.json";
        const string path2 = "users/p2.json";

        await manager.StageSaveOperationAsync(path1, new TransactionTestEntity { Id = "p1", Name = "P1" }, null,
            CancellationToken.None);
        await manager.StageSaveOperationAsync(path2, new TransactionTestEntity { Id = "p2", Name = "P2" }, null,
            CancellationToken.None);

        await tx.CommitAsync();

        var manifestPath = $"__cloudstorageorm/tx/{tx.TransactionId:D}/manifest.json";
        var manifestJson = storage.GetRawJson(manifestPath);
        manifestJson.ShouldContain("\"appliedOperationCount\":2");
    }

    [Fact]
    public async Task Commit_WhenConditionalSaveHasStaleEtag_ThrowsDbUpdateConcurrencyException()
    {
        var storage = new InMemoryStorageProvider();
        var manager = new CloudStorageTransactionManager(storage);
        const string userPath = "users/stale-save.json";

        await storage.SaveAsync(userPath, new TransactionTestEntity { Id = "stale", Name = "V1" });
        var original = await storage.ReadWithMetadataAsync<TransactionTestEntity>(userPath);

        var tx = await manager.BeginTransactionAsync();
        await manager.StageSaveOperationAsync(
            userPath,
            new TransactionTestEntity { Id = "stale", Name = "V2" },
            original.ETag,
            CancellationToken.None);

        await storage.SaveAsync(userPath, new TransactionTestEntity { Id = "stale", Name = "Concurrent" });

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => tx.CommitAsync());
    }

    [Fact]
    public async Task Commit_WhenConditionalDeleteHasStaleEtag_ThrowsDbUpdateConcurrencyException()
    {
        var storage = new InMemoryStorageProvider();
        var manager = new CloudStorageTransactionManager(storage);
        const string userPath = "users/stale-delete.json";

        await storage.SaveAsync(userPath, new TransactionTestEntity { Id = "stale-del", Name = "V1" });
        var original = await storage.ReadWithMetadataAsync<TransactionTestEntity>(userPath);

        var tx = await manager.BeginTransactionAsync();
        await manager.StageDeleteOperationAsync(userPath, original.ETag, CancellationToken.None);

        await storage.SaveAsync(userPath, new TransactionTestEntity { Id = "stale-del", Name = "Concurrent" });

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => tx.CommitAsync());
    }

    private sealed class InMemoryStorageProvider : IStorageProvider
    {
        private readonly Lock _sync = new();
        private readonly Dictionary<string, string> _store = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _etags = new(StringComparer.Ordinal);
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        // ReSharper disable once CollectionNeverQueried.Local
        public List<(string Path, string? IfMatchETag)> ConditionalSaveRequests { get; } = [];
        public List<(string Path, object Entity)> SaveRequests { get; } = [];

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
                SaveRequests.Add((path, entity!));
                _store[path] = JsonSerializer.Serialize(entity, _jsonOptions);
                _etags[path] = CreateNextEtag();
            }

            return Task.CompletedTask;
        }

        public Task<string?> SaveAsync<T>(string path, T entity, string? ifMatchETag)
        {
            lock (_sync)
            {
                ConditionalSaveRequests.Add((path, ifMatchETag));

                if (!string.IsNullOrWhiteSpace(ifMatchETag)
                    && (!_etags.TryGetValue(path, out var currentEtag)
                        || !string.Equals(currentEtag, ifMatchETag, StringComparison.Ordinal)))
                {
                    throw new StoragePreconditionFailedException(path);
                }

                SaveRequests.Add((path, entity!));
                _store[path] = JsonSerializer.Serialize(entity, _jsonOptions);
                var nextEtag = CreateNextEtag();
                _etags[path] = nextEtag;
                return Task.FromResult<string?>(nextEtag);
            }
        }

        public Task<T> ReadAsync<T>(string path)
        {
            lock (_sync)
            {
                return Task.FromResult(!_store.TryGetValue(path, out var json) 
                    ? default(T)! 
                    : JsonSerializer.Deserialize<T>(json, _jsonOptions)!)!;
            }
        }

        public async Task<StorageObject<T>> ReadWithMetadataAsync<T>(string path)
        {
            var value = await ReadAsync<T>(path);
            var exists = !EqualityComparer<T>.Default.Equals(value, default!);
            lock (_sync)
            {
                _etags.TryGetValue(path, out var eTag);
                return new StorageObject<T>(value, exists ? eTag : null, exists);
            }
        }

        public Task DeleteAsync(string path)
        {
            lock (_sync)
            {
                _store.Remove(path);
                _etags.Remove(path);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string path, string? ifMatchETag)
        {
            lock (_sync)
            {
                if (!string.IsNullOrWhiteSpace(ifMatchETag)
                    && (!_etags.TryGetValue(path, out var currentEtag)
                        || !string.Equals(currentEtag, ifMatchETag, StringComparison.Ordinal)))
                {
                    throw new StoragePreconditionFailedException(path);
                }

                _store.Remove(path);
                _etags.Remove(path);
            }

            return Task.CompletedTask;
        }

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
                _etags[path] = CreateNextEtag();
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

        private static string CreateNextEtag() => $"in-memory-etag-{Guid.NewGuid():N}";
    }

    private sealed class TransactionTestEntity
    {
        // ReSharper disable once PropertyCanBeMadeInitOnly.Local
        public string Id { get; set; } = string.Empty;

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string Name { get; set; } = string.Empty;
    }
}