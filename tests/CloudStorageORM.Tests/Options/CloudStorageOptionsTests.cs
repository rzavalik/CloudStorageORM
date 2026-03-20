using CloudStorageORM.Options;
using Shouldly;

namespace CloudStorageORM.Tests.Options;

public class CloudStorageOptionsTests
{
    [Fact]
    public void AzureAndAwsSpecificFields_AreAssignable()
    {
        var options = new CloudStorageOptions
        {
            Azure =
            {
                ConnectionString = "UseDevelopmentStorage=true"
            },
            Aws =
            {
                AccessKeyId = "test-access-key",
                SecretAccessKey = "test-secret-key",
                Region = "us-east-1",
                ServiceUrl = "http://localhost:4566",
                ForcePathStyle = true
            }
        };

        options.Azure.ConnectionString.ShouldBe("UseDevelopmentStorage=true");
        options.Aws.AccessKeyId.ShouldBe("test-access-key");
        options.Aws.SecretAccessKey.ShouldBe("test-secret-key");
        options.Aws.Region.ShouldBe("us-east-1");
        options.Aws.ServiceUrl.ShouldBe("http://localhost:4566");
        options.Aws.ForcePathStyle.ShouldBeTrue();
    }

    [Fact]
    public void ProviderSpecificOptions_AreInitialized()
    {
        var options = new CloudStorageOptions();

        options.Azure.ShouldNotBeNull();
        options.Aws.ShouldNotBeNull();
    }
}