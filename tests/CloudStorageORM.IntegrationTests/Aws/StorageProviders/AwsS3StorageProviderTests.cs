using System.ComponentModel.DataAnnotations;
using CloudStorageORM.Abstractions;
using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.IntegrationTests.Aws;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Options;
using CloudStorageORM.Providers.Aws.StorageProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Shouldly;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
// ReSharper disable PropertyCanBeMadeInitOnly.Local

namespace CloudStorageORM.IntegrationTests.AWS.Aws.StorageProviders;

public class AwsS3StorageProviderTests(LocalStackFixture fixture) : IClassFixture<LocalStackFixture>
{
    [Fact]
    public async Task SaveAsync_ShouldSaveEntity()
    {
        fixture.EnsureAvailableOrSkip();

        var path = BuildPath("save");
        var entity = new TestEntity { Id = "entity1", Name = "Test" };

        await fixture.Provider.SaveAsync(path, entity);

        var savedEntity = await fixture.Provider.ReadAsync<TestEntity>(path);

        savedEntity.ShouldNotBeNull();
        savedEntity.Id.ShouldBe(entity.Id);
        savedEntity.Name.ShouldBe(entity.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteEntity()
    {
        fixture.EnsureAvailableOrSkip();

        var path = BuildPath("delete");
        var entity = new TestEntity { Id = "entity2", Name = "ToDelete" };

        await fixture.Provider.SaveAsync(path, entity);
        await fixture.Provider.DeleteAsync(path);

        var deletedEntity = await fixture.Provider.ReadAsync<TestEntity>(path);

        deletedEntity.ShouldBeNull();
    }

    [Fact]
    public async Task ListAsync_ShouldReturnListOfEntities()
    {
        fixture.EnsureAvailableOrSkip();

        var prefix = $"test-folder/{Guid.NewGuid():N}/";
        await fixture.Provider.SaveAsync($"{prefix}list-entity1.json", new TestEntity { Id = "list1", Name = "Item1" });
        await fixture.Provider.SaveAsync($"{prefix}list-entity2.json", new TestEntity { Id = "list2", Name = "Item2" });

        var files = await fixture.Provider.ListAsync(prefix);

        files.ShouldNotBeEmpty();
        files.Count.ShouldBeGreaterThanOrEqualTo(2);
        files.ShouldContain(x => x.EndsWith("list-entity1.json"));
        files.ShouldContain(x => x.EndsWith("list-entity2.json"));
    }

    [Fact]
    public async Task SaveAsync_WithStaleEtag_ShouldThrowStoragePreconditionFailedException()
    {
        fixture.EnsureAvailableOrSkip();

        var path = BuildPath("concurrency");
        await fixture.Provider.SaveAsync(path, new TestEntity { Id = "etag", Name = "v1" });
        var original = await fixture.Provider.ReadWithMetadataAsync<TestEntity>(path);

        await fixture.Provider.SaveAsync(path, new TestEntity { Id = "etag", Name = "v2" });

        var ex = await Should.ThrowAsync<Exception>(() =>
            fixture.Provider.SaveAsync(path, new TestEntity { Id = "etag", Name = "v3" }, original.ETag));

        ex.Message.ShouldContain("changed by another writer");
    }

    [Fact]
    public async Task TransactionalCommit_WithStaleEtagOnSave_ShouldThrowDbUpdateConcurrencyException()
    {
        fixture.EnsureAvailableOrSkip();

        var (provider, bucketName) = await CreateIsolatedProviderAsync(fixture, "save");
        var id = $"tx-save-{Guid.NewGuid():N}";
        var path = new BlobPathResolver(provider).GetPath(typeof(AwsTransactionalEntity), id);

        await provider.SaveAsync(path, new AwsTransactionalEntity { Id = id, Name = "v1" });
        var original = await provider.ReadWithMetadataAsync<AwsTransactionalEntity>(path);

        await using var context = CreateTransactionContext(fixture, bucketName);
        var tracked = new AwsTransactionalEntity { Id = id, Name = "tx-update", ETag = original.ETag };
        context.Attach(tracked);
        context.Entry(tracked).Property(x => x.ETag).OriginalValue = original.ETag;
        context.Entry(tracked).State = EntityState.Modified;

        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.SaveChangesAsync();

        await provider.SaveAsync(path, new AwsTransactionalEntity { Id = id, Name = "v2" });

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => transaction.CommitAsync());
    }

    [Fact]
    public async Task TransactionalCommit_WithStaleEtagOnDelete_ShouldThrowDbUpdateConcurrencyException()
    {
        fixture.EnsureAvailableOrSkip();

        var (provider, bucketName) = await CreateIsolatedProviderAsync(fixture, "delete");
        var id = $"tx-delete-{Guid.NewGuid():N}";
        var path = new BlobPathResolver(provider).GetPath(typeof(AwsTransactionalEntity), id);

        await provider.SaveAsync(path, new AwsTransactionalEntity { Id = id, Name = "v1" });
        var original = await provider.ReadWithMetadataAsync<AwsTransactionalEntity>(path);

        await using var context = CreateTransactionContext(fixture, bucketName);
        var tracked = new AwsTransactionalEntity { Id = id, Name = "to-delete", ETag = original.ETag };
        context.Attach(tracked);
        context.Entry(tracked).Property(x => x.ETag).OriginalValue = original.ETag;
        context.Remove(tracked);

        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.SaveChangesAsync();

        await provider.SaveAsync(path, new AwsTransactionalEntity { Id = id, Name = "v2" });

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => transaction.CommitAsync());
    }

    [Fact]
    public async Task RetryEnabled_SaveChangesAndPrimaryKeyQuery_RecoverFromInjectedTransientFailure()
    {
        fixture.EnsureAvailableOrSkip();

        var (innerProvider, bucketName) = await CreateIsolatedProviderAsync(fixture, "retry");
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Aws,
            ContainerName = bucketName,
            Aws = new CloudStorageAwsOptions
            {
                AccessKeyId = fixture.AccessKeyId,
                SecretAccessKey = fixture.SecretAccessKey,
                Region = fixture.Region,
                ServiceUrl = fixture.ServiceUrl,
                ForcePathStyle = true
            },
            Retry = new CloudStorageRetryOptions
            {
                Enabled = true,
                MaxRetries = 2,
                BaseDelay = TimeSpan.Zero,
                MaxDelay = TimeSpan.Zero,
                JitterFactor = 0
            }
        };

        var flakyProvider = new TransientOnceStorageProvider(innerProvider, failSaveOnce: true, failReadOnce: true);
        var dbContextOptions = new DbContextOptionsBuilder<RetryHarnessDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new RetryHarnessDbContext(dbContextOptions);

        var database = new CloudStorageDatabase(
            context.Model,
            new NoOpDatabaseCreator(),
            new NoOpExecutionStrategyFactory(),
            flakyProvider,
            new CurrentDbContextStub(context),
            new BlobPathResolver(flakyProvider),
            new CloudStorageTransactionManager(),
            options);

        var entity = new RetryHarnessEntity { Id = $"retry-{Guid.NewGuid():N}", Name = "before" };
        context.Add(entity);
        var entries = context
            .ChangeTracker
            .Entries()
            .Select(e => (IUpdateEntry)e.GetInfrastructure())
            .ToList();

        var saved = await database.SaveChangesAsync(entries);
        saved.ShouldBe(1);

        var queryProvider = new CloudStorageQueryProvider(database, new BlobPathResolver(flakyProvider));
        var queryable = new CloudStorageQueryable<RetryHarnessEntity>(queryProvider);
        var loaded = queryable.FirstOrDefault(x => x.Id == entity.Id);

        loaded.ShouldNotBeNull();
        loaded.Name.ShouldBe("before");
        flakyProvider.SaveAttempts.ShouldBeGreaterThanOrEqualTo(2);
        flakyProvider.ReadAttempts.ShouldBeGreaterThanOrEqualTo(2);
    }

    private static AwsTransactionalTestDbContext CreateTransactionContext(LocalStackFixture fixture, string bucketName)
    {
        var options = new DbContextOptionsBuilder<AwsTransactionalTestDbContext>()
            .UseCloudStorageOrm(cfg =>
            {
                cfg.Provider = CloudProvider.Aws;
                cfg.ContainerName = bucketName;
                cfg.Aws = new CloudStorageAwsOptions
                {
                    AccessKeyId = fixture.AccessKeyId,
                    SecretAccessKey = fixture.SecretAccessKey,
                    Region = fixture.Region,
                    ServiceUrl = fixture.ServiceUrl,
                    ForcePathStyle = true
                };
            })
            .Options;

        return new AwsTransactionalTestDbContext(options);
    }

    private static async Task<(AwsS3StorageProvider Provider, string BucketName)> CreateIsolatedProviderAsync(
        LocalStackFixture fixture,
        string scenario)
    {
        var rawBucketName = $"tx-{scenario}-{Guid.NewGuid():N}";
        var bucketName = rawBucketName.Length <= 45 ? rawBucketName : rawBucketName[..45];
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Aws,
            ContainerName = bucketName,
            Aws = new CloudStorageAwsOptions
            {
                AccessKeyId = fixture.AccessKeyId,
                SecretAccessKey = fixture.SecretAccessKey,
                Region = fixture.Region,
                ServiceUrl = fixture.ServiceUrl,
                ForcePathStyle = true
            }
        };

        var provider = new AwsS3StorageProvider(options);
        await provider.CreateContainerIfNotExistsAsync();
        return (provider, bucketName);
    }

    private static string BuildPath(string scenario)
    {
        return $"test-folder/{scenario}-{Guid.NewGuid():N}.json";
    }

    private sealed class TestEntity
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }

    private sealed class AwsTransactionalEntity
    {
        [MaxLength(100)] public string Id { get; init; } = string.Empty;

        [MaxLength(100)] public string Name { get; set; } = string.Empty;

        [MaxLength(256)] public string? ETag { get; set; }
    }

    private sealed class AwsTransactionalTestDbContext(DbContextOptions<AwsTransactionalTestDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AwsTransactionalEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<AwsTransactionalEntity>().UseObjectETagConcurrency();
        }
    }

    private sealed class RetryHarnessDbContext(DbContextOptions<RetryHarnessDbContext> options) : DbContext(options)
    {
        public DbSet<RetryHarnessEntity> Entities => Set<RetryHarnessEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RetryHarnessEntity>().HasKey(x => x.Id);
        }
    }

    private sealed class RetryHarnessEntity
    {
        [MaxLength(100)] public string Id { get; init; } = string.Empty;

        [MaxLength(100)] public string Name { get; set; } = string.Empty;
    }

    private sealed class TransientOnceStorageProvider(
        IStorageProvider inner,
        bool failSaveOnce,
        bool failReadOnce) : IStorageProvider
    {
        private int _saveFailuresRemaining = failSaveOnce ? 1 : 0;
        private int _readFailuresRemaining = failReadOnce ? 1 : 0;

        public int SaveAttempts { get; private set; }
        public int ReadAttempts { get; private set; }

        public CloudProvider CloudProvider => inner.CloudProvider;

        public Task DeleteContainerAsync() => inner.DeleteContainerAsync();

        public Task CreateContainerIfNotExistsAsync() => inner.CreateContainerIfNotExistsAsync();

        public async Task SaveAsync<T>(string path, T entity)
        {
            SaveAttempts++;
            if (_saveFailuresRemaining > 0)
            {
                _saveFailuresRemaining--;
                throw new HttpRequestException("Injected transient save failure.");
            }

            await inner.SaveAsync(path, entity);
        }

        public async Task<string?> SaveAsync<T>(string path, T entity, string? ifMatchETag)
        {
            SaveAttempts++;
            if (_saveFailuresRemaining > 0)
            {
                _saveFailuresRemaining--;
                throw new HttpRequestException("Injected transient save failure.");
            }

            return await inner.SaveAsync(path, entity, ifMatchETag);
        }

        public Task<T> ReadAsync<T>(string path) => inner.ReadAsync<T>(path);

        public async Task<StorageObject<T>> ReadWithMetadataAsync<T>(string path)
        {
            ReadAttempts++;
            if (_readFailuresRemaining > 0)
            {
                _readFailuresRemaining--;
                throw new HttpRequestException("Injected transient read failure.");
            }

            return await inner.ReadWithMetadataAsync<T>(path);
        }

        public Task DeleteAsync(string path) => inner.DeleteAsync(path);

        public Task DeleteAsync(string path, string? ifMatchETag) => inner.DeleteAsync(path, ifMatchETag);

        public Task<List<string>> ListAsync(string folderPath) => inner.ListAsync(folderPath);

        public Task<StorageListPage> ListPageAsync(
            string folderPath,
            int pageSize,
            string? continuationToken,
            CancellationToken cancellationToken = default)
        {
            return inner.ListPageAsync(folderPath, pageSize, continuationToken, cancellationToken);
        }

        public string SanitizeBlobName(string rawName) => inner.SanitizeBlobName(rawName);
    }

    private sealed class NoOpDatabaseCreator : IDatabaseCreator
    {
        public bool EnsureDeleted() => true;

        public Task<bool> EnsureDeletedAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public bool EnsureCreated() => true;

        public Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public bool CanConnect() => true;

        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public bool HasTables() => true;

        public Task<bool> HasTablesAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class NoOpExecutionStrategyFactory : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create()
        {
            throw new NotSupportedException("Execution strategy is not used by this integration harness.");
        }
    }

    private sealed class CurrentDbContextStub(DbContext context) : ICurrentDbContext
    {
        public DbContext Context => context;
    }
}