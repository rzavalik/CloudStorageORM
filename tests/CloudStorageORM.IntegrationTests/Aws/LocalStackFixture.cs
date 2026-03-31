using CloudStorageORM.Enums;
using CloudStorageORM.Options;
using CloudStorageORM.Providers.Aws.StorageProviders;

namespace CloudStorageORM.IntegrationTests.Azure.Aws;

public sealed class LocalStackFixture : IAsyncLifetime
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(2)
    };

    private AwsS3StorageProvider? _provider;

    public string ServiceUrl { get; } = Environment.GetEnvironmentVariable("CLOUDSTORAGEORM_AWS_SERVICE_URL")
                                        ?? "http://localhost:4566";

    public string AccessKeyId { get; } = Environment.GetEnvironmentVariable("CLOUDSTORAGEORM_AWS_ACCESS_KEY_ID")
                                         ?? "test";

    public string SecretAccessKey { get; } = Environment.GetEnvironmentVariable("CLOUDSTORAGEORM_AWS_SECRET_ACCESS_KEY")
                                             ?? "test";

    public string Region { get; } = Environment.GetEnvironmentVariable("CLOUDSTORAGEORM_AWS_REGION")
                                    ?? "us-east-1";

    public string BucketName { get; } = Environment.GetEnvironmentVariable("CLOUDSTORAGEORM_AWS_BUCKET")
                                        ?? "cloudstorageorm-integration-tests";

    public string? SkipReason { get; private set; }

    public bool IsAvailable => SkipReason is null;

    public AwsS3StorageProvider Provider
    {
        get
        {
            EnsureAvailableOrSkip();
            return _provider!;
        }
    }

    public async Task InitializeAsync()
    {
        if (!await IsEndpointReachableAsync(ServiceUrl))
        {
            SkipReason =
                $"LocalStack is not reachable at '{ServiceUrl}'. Set CLOUDSTORAGEORM_AWS_SERVICE_URL or start LocalStack.";
            return;
        }

        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Aws,
            ContainerName = BucketName,
            Aws = new CloudStorageAwsOptions
            {
                AccessKeyId = AccessKeyId,
                SecretAccessKey = SecretAccessKey,
                Region = Region,
                ServiceUrl = ServiceUrl,
                ForcePathStyle = true
            }
        };

        try
        {
            _provider = new AwsS3StorageProvider(options);
            await _provider.CreateContainerIfNotExistsAsync();
        }
        catch (Exception ex)
        {
            SkipReason = $"Unable to initialize AWS integration fixture against LocalStack: {ex.Message}";
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void EnsureAvailableOrSkip()
    {
        IntegrationTestSkip.IfUnavailable(SkipReason);
    }

    private static async Task<bool> IsEndpointReachableAsync(string serviceUrl)
    {
        try
        {
            using var response = await HttpClient.GetAsync(serviceUrl);
            return true;
        }
        catch
        {
            return false;
        }
    }
}