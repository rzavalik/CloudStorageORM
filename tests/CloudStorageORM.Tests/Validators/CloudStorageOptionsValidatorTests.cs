using CloudStorageORM.Enums;
using CloudStorageORM.Options;
using CloudStorageORM.Validators;
using Shouldly;

namespace CloudStorageORM.Tests.Validators;

public class CloudStorageOptionsValidatorTests
{
    [Fact]
    public void Validate_WithValidAzureOptions_DoesNotThrow()
    {
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ContainerName = "unit-container",
            Azure = new CloudStorageAzureOptions
            {
                ConnectionString = "UseDevelopmentStorage=true"
            }
        };

        Should.NotThrow(() => CloudStorageOptionsValidator.Validate(options));
    }

    [Fact]
    public void Validate_WithMissingAzureConnectionString_Throws()
    {
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ContainerName = "unit-container",
            Azure = new CloudStorageAzureOptions
            {
                ConnectionString = ""
            }
        };

        var ex = Should.Throw<InvalidOperationException>(() => CloudStorageOptionsValidator.Validate(options));
        ex.Message.ShouldContain("Azure.ConnectionString");
    }

    [Fact]
    public void Validate_WithValidAwsOptions_DoesNotThrow()
    {
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Aws,
            ContainerName = "unit-bucket",
            Aws = new CloudStorageAwsOptions
            {
                AccessKeyId = "test-key",
                SecretAccessKey = "test-secret",
                Region = "us-east-1"
            }
        };

        Should.NotThrow(() => CloudStorageOptionsValidator.Validate(options));
    }

    [Fact]
    public void Validate_WithMissingAwsRegion_Throws()
    {
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Aws,
            ContainerName = "unit-bucket",
            Aws = new CloudStorageAwsOptions
            {
                AccessKeyId = "test-key",
                SecretAccessKey = "test-secret",
                Region = ""
            }
        };

        var ex = Should.Throw<InvalidOperationException>(() => CloudStorageOptionsValidator.Validate(options));
        ex.Message.ShouldContain("Aws.Region");
    }

    [Fact]
    public void Validate_WithMissingContainerName_Throws()
    {
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Aws,
            ContainerName = "",
            Aws = new CloudStorageAwsOptions
            {
                AccessKeyId = "test-key",
                SecretAccessKey = "test-secret",
                Region = "us-east-1"
            }
        };

        var ex = Should.Throw<InvalidOperationException>(() => CloudStorageOptionsValidator.Validate(options));
        ex.Message.ShouldContain("ContainerName");
    }
}