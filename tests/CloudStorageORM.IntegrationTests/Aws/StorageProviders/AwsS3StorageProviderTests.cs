using Shouldly;

namespace CloudStorageORM.IntegrationTests.Azure.Aws.StorageProviders;

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

    private static string BuildPath(string scenario)
    {
        return $"test-folder/{scenario}-{Guid.NewGuid():N}.json";
    }

    private sealed class TestEntity
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }
}