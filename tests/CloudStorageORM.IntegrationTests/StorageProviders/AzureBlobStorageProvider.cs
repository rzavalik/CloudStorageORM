namespace CloudStorageORM.IntegrationTests.StorageProviders.Azure
{
    using System.Threading.Tasks;
    using CloudStorageORM.IntegrationTests.Azure.Helpers;
    using CloudStorageORM.Providers.Azure.StorageProviders;
    using Shouldly;
    using Xunit;

    public class AzureBlobStorageProviderTests : IClassFixture<StorageFixture>
    {
        private readonly AzureBlobStorageProvider _provider;
        private readonly StorageFixture _fixture;

        public AzureBlobStorageProviderTests(StorageFixture fixture)
        {
            _fixture = fixture;
            _provider = new AzureBlobStorageProvider(_fixture.ConnectionString, _fixture.ContainerName);
        }

        [Fact]
        public async Task SaveAsync_ShouldSaveEntity()
        {
            var entity = new TestEntity { Id = "entity1", Name = "Test" };
            await _provider.SaveAsync("test-folder/entity1.json", entity);

            var savedEntity = await _provider.ReadAsync<TestEntity>("test-folder/entity1.json");

            savedEntity.ShouldNotBeNull();
            savedEntity.Id.ShouldBe(entity.Id);
            savedEntity.Name.ShouldBe(entity.Name);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteEntity()
        {
            var entity = new TestEntity { Id = "entity2", Name = "ToDelete" };
            await _provider.SaveAsync("test-folder/entity2.json", entity);

            await _provider.DeleteAsync("test-folder/entity2.json");

            var deletedEntity = await _provider.ReadAsync<TestEntity>("test-folder/entity2.json");

            deletedEntity.ShouldBeNull();
        }

        [Fact]
        public async Task ListAsync_ShouldReturnListOfEntities()
        {
            await _provider.SaveAsync("test-folder/list-entity1.json", new TestEntity { Id = "list1", Name = "Item1" });
            await _provider.SaveAsync("test-folder/list-entity2.json", new TestEntity { Id = "list2", Name = "Item2" });

            var files = await _provider.ListAsync("test-folder/");

            files.ShouldNotBeEmpty();
            files.Count.ShouldBeGreaterThanOrEqualTo(2);
        }
    }

    public class TestEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
