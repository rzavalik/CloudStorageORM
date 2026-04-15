using System.ComponentModel.DataAnnotations;
using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Options;
using CloudStorageORM.Providers.Azure.StorageProviders;
using Microsoft.EntityFrameworkCore;
using Shouldly;

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

    private static AzureTransactionalTestDbContext CreateTransactionContext(string connectionString, string containerName)
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
    [MaxLength(100)]
    public string Id { get; init; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? ETag { get; set; }
}
