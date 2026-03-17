using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Options;
using Shouldly;

namespace CloudStorageORM.Tests.Extensions;

public class CloudStorageOrmOptionsExtensionInfoTests
{
    [Fact]
    public void Properties_ReturnExpectedValues()
    {
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ContainerName = "test",
            Azure = new CloudStorageAzureOptions
            {
                ConnectionString = "UseDevelopmentStorage=true"
            }
        };

        var extension = new CloudStorageOrmOptionsExtension(options);
        var info = new CloudStorageOrmOptionsExtensionInfo(extension);

        info.IsDatabaseProvider.ShouldBeTrue();
        info.LogFragment.ShouldContain("CloudStorageORM");
        info.LogFragment.ShouldContain("Azure");
        info.GetServiceProviderHashCode().ShouldNotBe(0);
    }

    [Fact]
    public void Properties_WithAwsOptions_ReturnExpectedValues()
    {
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Aws,
            ContainerName = "aws-bucket",
            Aws = new CloudStorageAwsOptions
            {
                AccessKeyId = "test-key",
                SecretAccessKey = "test-secret",
                Region = "us-east-1",
                ServiceUrl = "http://localhost:4566",
                ForcePathStyle = true
            }
        };

        var extension = new CloudStorageOrmOptionsExtension(options);
        var info = new CloudStorageOrmOptionsExtensionInfo(extension);

        info.LogFragment.ShouldContain("Aws");
        info.LogFragment.ShouldContain("us-east-1");
        info.GetServiceProviderHashCode().ShouldNotBe(0);
    }

    [Fact]
    public void PopulateDebugInfo_DoesNotThrow()
    {
        var extension = new CloudStorageOrmOptionsExtension(new CloudStorageOptions());
        var info = new CloudStorageOrmOptionsExtensionInfo(extension);
        var debug = new Dictionary<string, string>();

        Should.NotThrow(() => info.PopulateDebugInfo(debug));
        debug.ShouldContainKey("CloudStorageORM:Provider");
        debug.ShouldContainKey("CloudStorageORM:ContainerName");
    }

    [Fact]
    public void ShouldUseSameServiceProvider_AlwaysFalse()
    {
        var extension = new CloudStorageOrmOptionsExtension(new CloudStorageOptions());
        var info = new CloudStorageOrmOptionsExtensionInfo(extension);

        var other = new CloudStorageOrmOptionsExtensionInfo(
            new CloudStorageOrmOptionsExtension(new CloudStorageOptions()));
        info.ShouldUseSameServiceProvider(other).ShouldBeFalse();
    }

    [Fact]
    public void ExtensionProperty_ReturnsUnderlyingExtension()
    {
        var extension = new CloudStorageOrmOptionsExtension(new CloudStorageOptions());
        var info = new CloudStorageOrmOptionsExtensionInfo(extension);

        info.Extension.ShouldBeSameAs(extension);
    }
}