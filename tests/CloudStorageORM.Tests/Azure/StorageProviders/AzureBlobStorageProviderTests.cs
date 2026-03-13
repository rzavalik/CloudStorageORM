using System.Reflection;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CloudStorageORM.Enums;
using CloudStorageORM.Options;
using CloudStorageORM.Providers.Azure.StorageProviders;
using Moq;
using Shouldly;

namespace CloudStorageORM.Tests.Azure.StorageProviders;

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
            .GetField("_containerClient",
                BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(sut, containerClientMock.Object);

        return sut;
    }

    [Fact]
    public void CloudProvider_ShouldBeAzure()
    {
        var sut = MakeSut(new Mock<BlobContainerClient>());
        sut.CloudProvider.ShouldBe(CloudProvider.Azure);
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
    public async Task ReadAsync_WhenBlobExistsButContentIsMissing_ThrowsNullReferenceException()
    {
        var containerClientMock = new Mock<BlobContainerClient>();
        var blobClientMock = new Mock<BlobClient>();

        containerClientMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(blobClientMock.Object);
        blobClientMock.Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        blobClientMock.Setup(x => x.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobDownloadResult(), Mock.Of<Response>()));

        var sut = MakeSut(containerClientMock);

        await Should.ThrowAsync<NullReferenceException>(
            () => sut.ReadAsync<TestEntity>("test-folder/entity3.json"));
    }

    [Fact]
    public async Task ReadAsync_WhenBlobDoesNotExist_ReturnsDefault()
    {
        var containerClientMock = new Mock<BlobContainerClient>();
        var blobClientMock = new Mock<BlobClient>();

        containerClientMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(blobClientMock.Object);
        blobClientMock.Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        var sut = MakeSut(containerClientMock);
        var result = await sut.ReadAsync<TestEntity>("test-folder/missing.json");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteContainerAsync_CallsDeleteIfExists()
    {
        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(x => x.DeleteIfExistsAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        var sut = MakeSut(containerClientMock);
        await sut.DeleteContainerAsync();

        containerClientMock.Verify(x => x.DeleteIfExistsAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateContainerIfNotExistsAsync_CallsCreateIfNotExists()
    {
        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(x => x.CreateIfNotExistsAsync(PublicAccessType.None, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(Mock.Of<BlobContainerInfo>(), Mock.Of<Response>()));

        var sut = MakeSut(containerClientMock);
        await sut.CreateContainerIfNotExistsAsync();

        containerClientMock.Verify(
            x => x.CreateIfNotExistsAsync(PublicAccessType.None, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithOptionsAndBlobContainerClient_CallsCreateIfNotExists()
    {
        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(x => x.CreateIfNotExists(
                PublicAccessType.None,
                null,
                null,
                CancellationToken.None))
            .Returns(Mock.Of<Response<BlobContainerInfo>>());

        var sut = new AzureBlobStorageProvider(containerClientMock.Object);

        sut.CloudProvider.ShouldBe(CloudProvider.Azure);
        containerClientMock.Verify(
            x => x.CreateIfNotExists(PublicAccessType.None, null, null, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithConnectionStringAndContainerName_UsesConfiguredFactory()
    {
        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(x => x.CreateIfNotExists(
                PublicAccessType.None,
                null,
                null,
                default))
            .Returns(Mock.Of<Response<BlobContainerInfo>>());

        var previousFactory = AzureBlobStorageProvider.ConnectionContainerClientFactory;
        AzureBlobStorageProvider.ConnectionContainerClientFactory = (connectionString, containerName) =>
        {
            connectionString.ShouldBe("UseDevelopmentStorage=true");
            containerName.ShouldBe("ctor-connection-string");
            return containerClientMock.Object;
        };

        try
        {
            var sut = new AzureBlobStorageProvider("UseDevelopmentStorage=true", "ctor-connection-string");

            sut.CloudProvider.ShouldBe(CloudProvider.Azure);
            containerClientMock.Verify(
                x => x.CreateIfNotExists(PublicAccessType.None, null, null, default),
                Times.Once);
        }
        finally
        {
            AzureBlobStorageProvider.ConnectionContainerClientFactory = previousFactory;
        }
    }

    [Fact]
    public void SanitizeBlobName_ReplacesInvalidCharacters()
    {
        var sut = MakeSut(new Mock<BlobContainerClient>());
        var result = sut.SanitizeBlobName("A B/C+#`[\"]");
        result.ShouldBe(result.ToLowerInvariant());
        result.ShouldNotContain(" ");
        result.ShouldNotContain("/");
        result.ShouldNotContain("+");
        result.ShouldNotContain("#");
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

    private class MockAsyncPageable(IEnumerable<BlobItem> items) : AsyncPageable<BlobItem>
    {
        private readonly IReadOnlyList<BlobItem> _items = items.ToList();

        public override async IAsyncEnumerable<Page<BlobItem>> AsPages(string? continuationToken = null,
            int? pageSizeHint = null)
        {
            yield return Page<BlobItem>.FromValues(_items, null, Mock.Of<Response>());
            await Task.CompletedTask;
        }
    }

    private class TestEntity
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string Id { get; init; } = string.Empty;
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string Name { get; init; } = string.Empty;
    }
}