using CloudStorageORM.Enums;
using CloudStorageORM.Options;
using CloudStorageORM.Providers;
using CloudStorageORM.Providers.Aws.StorageProviders;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

public class ProviderFactoryTests
{
    [Fact]
    public void GetStorageProvider_Aws_ReturnsAwsProvider()
    {
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Aws,
            ContainerName = "provider-tests",
            Aws = new CloudStorageAwsOptions
            {
                AccessKeyId = "test-key",
                SecretAccessKey = "test-secret",
                Region = "us-east-1"
            }
        };

        var provider = ProviderFactory.GetStorageProvider(options);
        provider.ShouldBeOfType<AwsS3StorageProvider>();
    }

    [Theory]
    [InlineData(CloudProvider.Gcp)]
    public void GetStorageProvider_Unsupported_Throws(CloudProvider cloud)
    {
        var options = new CloudStorageOptions { Provider = cloud };
        var ex = Should.Throw<NotSupportedException>(() => ProviderFactory.GetStorageProvider(options));
        ex.Message.ShouldContain("not supported");
    }
}