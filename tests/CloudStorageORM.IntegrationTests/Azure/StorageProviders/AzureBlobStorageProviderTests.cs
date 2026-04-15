using CloudStorageORM.Providers.Azure.StorageProviders;
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
}

public class TestEntity
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}