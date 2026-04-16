using System.ComponentModel.DataAnnotations;
using CloudStorageORM.Abstractions;
using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Options;
using CloudStorageORM.Providers.Azure.StorageProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Shouldly;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
// ReSharper disable PropertyCanBeMadeInitOnly.Local

namespace CloudStorageORM.IntegrationTests.Azure.Azure.StorageProviders;

public class AzureBlobStorageProviderTests(StorageFixture fixture) : IClassFixture<StorageFixture>
{
    [Fact]
    public async Task SaveAsync_ShouldSaveEntity()
    {
        fixture.EnsureAvailableOrSkip();

        var provider = new AzureBlobStorageProvider(fixture.ConnectionString, fixture.ContainerName);
        var entity = new TestEntity { Id = "entity1", Name = "Test" };
        await provider.SaveAsync("test-folder/entity1.json", entity);

        var savedEntity = await provider.ReadAsync<TestEntity>("test-folder/entity1.json");

        savedEntity.ShouldNotBeNull();
        savedEntity.Id.ShouldBe(entity.Id);
        savedEntity.Name.ShouldBe(entity.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteEntity()
    {
        fixture.EnsureAvailableOrSkip();

        var provider = new AzureBlobStorageProvider(fixture.ConnectionString, fixture.ContainerName);
        var entity = new TestEntity { Id = "entity2", Name = "ToDelete" };
        await provider.SaveAsync("test-folder/entity2.json", entity);

        await provider.DeleteAsync("test-folder/entity2.json");

        var deletedEntity = await provider.ReadAsync<TestEntity>("test-folder/entity2.json");

        deletedEntity.ShouldBeNull();
    }

    [Fact]
    public async Task ListAsync_ShouldReturnListOfEntities()
    {
        fixture.EnsureAvailableOrSkip();

        var provider = new AzureBlobStorageProvider(fixture.ConnectionString, fixture.ContainerName);
        await provider.SaveAsync("test-folder/list-entity1.json", new TestEntity { Id = "list1", Name = "Item1" });
        await provider.SaveAsync("test-folder/list-entity2.json", new TestEntity { Id = "list2", Name = "Item2" });

        var files = await provider.ListAsync("test-folder/");

        files.ShouldNotBeEmpty();
        files.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task SaveAsync_WithStaleEtag_ShouldThrowStoragePreconditionFailedException()
    {
        fixture.EnsureAvailableOrSkip();

        var provider = new AzureBlobStorageProvider(fixture.ConnectionString, fixture.ContainerName);
        var path = $"test-folder/concurrency-{Guid.NewGuid():N}.json";

        await provider.SaveAsync(path, new TestEntity { Id = "etag", Name = "v1" });
        var original = await provider.ReadWithMetadataAsync<TestEntity>(path);

        await provider.SaveAsync(path, new TestEntity { Id = "etag", Name = "v2" });

        var ex = await Should.ThrowAsync<Exception>(() =>
            provider.SaveAsync(path, new TestEntity { Id = "etag", Name = "v3" }, original.ETag));

        ex.Message.ShouldContain("changed by another writer");
    }

    [Fact]
    public async Task TransactionalCommit_WithStaleEtagOnSave_ShouldThrowDbUpdateConcurrencyException()
    {
        fixture.EnsureAvailableOrSkip();

        var (provider, containerName) = await CreateIsolatedProviderAsync(fixture, "save");
        var id = $"tx-save-{Guid.NewGuid():N}";
        var path = new BlobPathResolver(provider).GetPath(typeof(TransactionTestEntity), id);

        await provider.SaveAsync(path, new TransactionTestEntity { Id = id, Name = "v1" });
        var original = await provider.ReadWithMetadataAsync<TransactionTestEntity>(path);

        await using var context = CreateTransactionContext(fixture.ConnectionString, containerName);
        var tracked = new TransactionTestEntity { Id = id, Name = "tx-update", ETag = original.ETag };
        context.Attach(tracked);
        context.Entry(tracked).Property(x => x.ETag).OriginalValue = original.ETag;
        context.Entry(tracked).State = EntityState.Modified;

        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.SaveChangesAsync();

        await provider.SaveAsync(path, new TransactionTestEntity { Id = id, Name = "v2" });

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => transaction.CommitAsync());
    }

    [Fact]
    public async Task TransactionalCommit_WithStaleEtagOnDelete_ShouldThrowDbUpdateConcurrencyException()
    {
        fixture.EnsureAvailableOrSkip();

        var (provider, containerName) = await CreateIsolatedProviderAsync(fixture, "delete");
        var id = $"tx-delete-{Guid.NewGuid():N}";
        var path = new BlobPathResolver(provider).GetPath(typeof(TransactionTestEntity), id);

        await provider.SaveAsync(path, new TransactionTestEntity { Id = id, Name = "v1" });
        var original = await provider.ReadWithMetadataAsync<TransactionTestEntity>(path);

        await using var context = CreateTransactionContext(fixture.ConnectionString, containerName);
        var tracked = new TransactionTestEntity { Id = id, Name = "to-delete", ETag = original.ETag };
        context.Attach(tracked);
        context.Entry(tracked).Property(x => x.ETag).OriginalValue = original.ETag;
        context.Remove(tracked);

        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.SaveChangesAsync();

        await provider.SaveAsync(path, new TransactionTestEntity { Id = id, Name = "v2" });

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => transaction.CommitAsync());
    }

    [Fact]
    public async Task RetryEnabled_SaveChangesAndPrimaryKeyQuery_RecoverFromInjectedTransientFailure()
    {
        fixture.EnsureAvailableOrSkip();

        var (innerProvider, containerName) = await CreateIsolatedProviderAsync(fixture, "retry");
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ContainerName = containerName,
            Azure = new CloudStorageAzureOptions
            {
                ConnectionString = fixture.ConnectionString
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

    private static AzureTransactionalTestDbContext CreateTransactionContext(string connectionString,
        string containerName)
    {
        var options = new DbContextOptionsBuilder<AzureTransactionalTestDbContext>()
            .UseCloudStorageOrm(cfg =>
            {
                cfg.Provider = CloudProvider.Azure;
                cfg.ContainerName = containerName;
                cfg.Azure = new CloudStorageAzureOptions
                {
                    ConnectionString = connectionString
                };
            })
            .Options;

        return new AzureTransactionalTestDbContext(options);
    }

    private static async Task<(AzureBlobStorageProvider Provider, string ContainerName)> CreateIsolatedProviderAsync(
        StorageFixture fixture,
        string scenario)
    {
        var containerName = $"tx-{scenario}-{Guid.NewGuid():N}"[..30];
        var provider = new AzureBlobStorageProvider(fixture.ConnectionString, containerName);
        await provider.CreateContainerIfNotExistsAsync();
        return (provider, containerName);
    }
}

public class TestEntity
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class AzureTransactionalTestDbContext(DbContextOptions<AzureTransactionalTestDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransactionTestEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<TransactionTestEntity>().UseObjectETagConcurrency();
    }
}

public sealed class TransactionTestEntity
{
    [MaxLength(100)] public string Id { get; init; } = string.Empty;

    [MaxLength(100)] public string Name { get; set; } = string.Empty;

    [MaxLength(256)] public string? ETag { get; set; }
}

public sealed class RetryHarnessDbContext(DbContextOptions<RetryHarnessDbContext> options) : DbContext(options)
{
    public DbSet<RetryHarnessEntity> Entities => Set<RetryHarnessEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RetryHarnessEntity>().HasKey(x => x.Id);
    }
}

public sealed class RetryHarnessEntity
{
    [MaxLength(100)] public string Id { get; init; } = string.Empty;

    [MaxLength(100)] public string Name { get; set; } = string.Empty;
}

internal sealed class TransientOnceStorageProvider(
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

internal sealed class NoOpDatabaseCreator : IDatabaseCreator
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

internal sealed class NoOpExecutionStrategyFactory : IExecutionStrategyFactory
{
    public IExecutionStrategy Create()
    {
        throw new NotSupportedException("Execution strategy is not used by this integration harness.");
    }
}

internal sealed class CurrentDbContextStub(DbContext context) : ICurrentDbContext
{
    public DbContext Context => context;
}