using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using CloudStorageORM.Enums;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Options;
using CloudStorageORM.Providers.Aws.StorageProviders;
using Moq;
using Shouldly;

namespace CloudStorageORM.Tests.Aws.StorageProviders;

public class AwsS3StorageProviderTests
{
    private const string BucketName = "test-bucket";

    private static CloudStorageOptions CreateOptions() => new()
    {
        Provider = CloudProvider.Aws,
        ContainerName = BucketName,
        Aws = new CloudStorageAwsOptions
        {
            AccessKeyId = "test-access-key",
            SecretAccessKey = "test-secret-key",
            Region = "us-east-1",
            ServiceUrl = "http://localhost:4566",
            ForcePathStyle = true
        }
    };

    private static AwsS3StorageProvider CreateSut(Mock<IAmazonS3> s3Mock)
    {
        return new AwsS3StorageProvider(CreateOptions(), s3Mock.Object);
    }

    [Fact]
    public void CloudProvider_ShouldBeAws()
    {
        var sut = CreateSut(new Mock<IAmazonS3>());

        sut.CloudProvider.ShouldBe(CloudProvider.Aws);
    }

    [Fact]
    public async Task SaveAsync_ShouldPutSerializedObject()
    {
        var s3Mock = new Mock<IAmazonS3>();
        PutObjectRequest? capturedRequest = null;
        s3Mock.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new PutObjectResponse());

        var sut = CreateSut(s3Mock);
        var entity = new TestEntity { Id = "id-1", Name = "Alice" };

        await sut.SaveAsync("users/id-1.json", entity);

        capturedRequest.ShouldNotBeNull();
        capturedRequest.BucketName.ShouldBe(BucketName);
        capturedRequest.Key.ShouldBe("users/id-1.json");
        capturedRequest.ContentType.ShouldBe("application/json");
        JsonSerializer.Deserialize<TestEntity>(capturedRequest.ContentBody)!.Name.ShouldBe("Alice");
    }

    [Fact]
    public async Task SaveAsync_WithIfMatch_ShouldSendConditionalRequest()
    {
        var s3Mock = new Mock<IAmazonS3>();
        PutObjectRequest? capturedRequest = null;
        s3Mock.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new PutObjectResponse());

        var sut = CreateSut(s3Mock);
        await sut.SaveAsync("users/id-1.json", new TestEntity { Id = "id-1", Name = "Alice" }, "etag-1");

        capturedRequest.ShouldNotBeNull();
        capturedRequest.IfMatch.ShouldBe("etag-1");
    }

    [Fact]
    public async Task SaveAsync_WithIfMatchAndPreconditionFailure_ShouldThrowStoragePreconditionFailedException()
    {
        var s3Mock = new Mock<IAmazonS3>();
        s3Mock.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("conflict")
            {
                StatusCode = HttpStatusCode.PreconditionFailed,
                ErrorCode = "PreconditionFailed"
            });

        var sut = CreateSut(s3Mock);

        await Should.ThrowAsync<StoragePreconditionFailedException>(() =>
            sut.SaveAsync("users/id-1.json", new TestEntity { Id = "id-1", Name = "Alice" }, "etag-1"));
    }

    [Fact]
    public async Task ReadAsync_WhenObjectExists_ShouldReturnEntity()
    {
        var s3Mock = new Mock<IAmazonS3>();
        var json = JsonSerializer.Serialize(new TestEntity { Id = "id-2", Name = "Bob" });
        var response = new GetObjectResponse
        {
            ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(json))
        };

        s3Mock.Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut(s3Mock);

        var result = await sut.ReadAsync<TestEntity>("users/id-2.json");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Bob");
    }

    [Fact]
    public async Task ReadAsync_WhenObjectDoesNotExist_ShouldReturnDefault()
    {
        var s3Mock = new Mock<IAmazonS3>();
        s3Mock.Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("missing")
            {
                StatusCode = HttpStatusCode.NotFound,
                ErrorCode = "NoSuchKey"
            });

        var sut = CreateSut(s3Mock);

        var result = await sut.ReadAsync<TestEntity>("users/missing.json");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteObject()
    {
        var s3Mock = new Mock<IAmazonS3>();
        DeleteObjectRequest? capturedRequest = null;
        s3Mock.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<DeleteObjectRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new DeleteObjectResponse());

        var sut = CreateSut(s3Mock);

        await sut.DeleteAsync("users/id-3.json");

        capturedRequest.ShouldNotBeNull();
        capturedRequest.BucketName.ShouldBe(BucketName);
        capturedRequest.Key.ShouldBe("users/id-3.json");
    }

    [Fact]
    public async Task DeleteAsync_WithIfMatch_ShouldSendConditionalRequest()
    {
        var s3Mock = new Mock<IAmazonS3>();
        DeleteObjectRequest? capturedRequest = null;
        s3Mock.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<DeleteObjectRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new DeleteObjectResponse());

        var sut = CreateSut(s3Mock);
        await sut.DeleteAsync("users/id-3.json", "etag-3");

        capturedRequest.ShouldNotBeNull();
        capturedRequest.IfMatch.ShouldBe("etag-3");
    }

    [Fact]
    public async Task ListAsync_ShouldReturnObjectKeys()
    {
        var s3Mock = new Mock<IAmazonS3>();
        s3Mock.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                IsTruncated = false,
                S3Objects =
                [
                    new S3Object { Key = "users/id-1.json" },
                    new S3Object { Key = "users/id-2.json" }
                ]
            });

        var sut = CreateSut(s3Mock);

        var result = await sut.ListAsync("users/");

        result.Count.ShouldBe(2);
        result.ShouldContain("users/id-1.json");
        result.ShouldContain("users/id-2.json");
    }


    [Fact]
    public async Task DeleteContainerAsync_WhenBucketDoesNotExist_ShouldNotThrow()
    {
        var s3Mock = new Mock<IAmazonS3>();
        s3Mock.Setup(x => x.DeleteBucketAsync(It.IsAny<DeleteBucketRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("missing") { StatusCode = HttpStatusCode.NotFound });

        var sut = CreateSut(s3Mock);

        await Should.NotThrowAsync(sut.DeleteContainerAsync);
    }

    [Fact]
    public void SanitizeBlobName_ReplacesInvalidCharacters()
    {
        var sut = CreateSut(new Mock<IAmazonS3>());

        var result = sut.SanitizeBlobName("A B/C+#`[\"]");

        result.ShouldBe(result.ToLowerInvariant());
        result.ShouldNotContain(" ");
        result.ShouldNotContain("/");
        result.ShouldNotContain("+");
        result.ShouldNotContain("#");
    }

    private sealed class TestEntity
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }
}