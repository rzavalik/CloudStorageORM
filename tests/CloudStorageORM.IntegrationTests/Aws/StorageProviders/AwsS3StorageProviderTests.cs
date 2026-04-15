using System.ComponentModel.DataAnnotations;
using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.IntegrationTests.Aws;
using CloudStorageORM.Options;
using CloudStorageORM.Providers.Aws.StorageProviders;
using Microsoft.EntityFrameworkCore;
using Shouldly;

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
        [MaxLength(100)]
        public string Id { get; init; } = string.Empty;

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? ETag { get; set; }
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
}