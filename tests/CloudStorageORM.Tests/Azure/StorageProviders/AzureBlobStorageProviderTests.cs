namespace CloudStorageORM.Tests.Azure.StorageProviders
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using CloudStorageORM.Azure.StorageProviders;
    using CloudStorageORM.Enums;
    using CloudStorageORM.Options;
    using global::Azure;
    using global::Azure.Storage.Blobs;
    using global::Azure.Storage.Blobs.Models;
    using Moq;
    using Shouldly;
    using Xunit;

    public class AzureBlobStorageProviderTests
    {
        private AzureBlobStorageProvider MakeSut(Mock<BlobContainerClient> containerClientMock)
        {
            var options = new CloudStorageOptions
            {
                Provider = CloudProvider.Azure,
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "test-container"
            };

            var sut = new AzureBlobStorageProvider(options);

            typeof(AzureBlobStorageProvider)
                .GetField("_containerClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(sut, containerClientMock.Object);

            return sut;
        }

        [Fact]
        public async Task SaveAsync_ShouldSaveEntity()
        {
            var containerClientMock = new Mock<BlobContainerClient>();
            var blobClientMock = new Mock<BlobClient>();

            containerClientMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(blobClientMock.Object);

            blobClientMock
                .Setup(x => x.UploadAsync(
                    It.IsAny<Stream>(),
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var sut = MakeSut(containerClientMock);

            var entity = new TestEntity { Id = "entity1", Name = "Test" };
            await sut.SaveAsync("test-folder/entity1.json", entity);

            blobClientMock.Verify(x => x.UploadAsync(
                It.IsAny<Stream>(),
                true,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteEntity()
        {
            var containerClientMock = new Mock<BlobContainerClient>();
            var blobClientMock = new Mock<BlobClient>();

            containerClientMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(blobClientMock.Object);

            blobClientMock
                .Setup(x => x.DeleteIfExistsAsync(
                    DeleteSnapshotsOption.IncludeSnapshots,
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            var sut = MakeSut(containerClientMock);

            await sut.DeleteAsync("test-folder/entity2.json");

            blobClientMock.Verify(x => x.DeleteIfExistsAsync(
                DeleteSnapshotsOption.None,
                null,
                It.IsAny<CancellationToken>()), Times.Once);

        }

        [Fact]
        public async Task ListAsync_ShouldReturnListOfEntities()
        {
            var containerClientMock = new Mock<BlobContainerClient>();
            var blobs = new[]
            {
                BlobsModelFactory.BlobItem(name: "test-folder/list-entity1.json"),
                BlobsModelFactory.BlobItem(name: "test-folder/list-entity2.json")
            };

            containerClientMock
                .Setup(x => x.GetBlobsAsync(
                    BlobTraits.None,
                    BlobStates.None,
                    "test-folder/",
                    It.IsAny<CancellationToken>()))
                .Returns(new MockAsyncPageable(blobs));

            var sut = MakeSut(containerClientMock);

            var files = await sut.ListAsync("test-folder/");

            files.ShouldNotBeEmpty();
            files.Count.ShouldBe(2);
            files.ShouldContain("test-folder/list-entity1.json");
            files.ShouldContain("test-folder/list-entity2.json");
        }

        private class MockAsyncPageable : AsyncPageable<BlobItem>
        {
            private readonly IReadOnlyList<BlobItem> _items;

            public MockAsyncPageable(IEnumerable<BlobItem> items)
            {
                _items = items.ToList();
            }

            public override async IAsyncEnumerable<Page<BlobItem>> AsPages(string continuationToken = null, int? pageSizeHint = null)
            {
                yield return Page<BlobItem>.FromValues(_items, null, Mock.Of<Response>());
                await Task.CompletedTask;
            }
        }

        private class TestEntity
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
    }
}
