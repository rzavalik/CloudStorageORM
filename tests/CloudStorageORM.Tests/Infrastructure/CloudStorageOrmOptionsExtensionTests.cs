using CloudStorageORM.Enums;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Options;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

public class CloudStorageOrmOptionsExtensionTests
{
    [Fact]
    public void Info_And_Options_AreExposed()
    {
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ContainerName = "unit",
            Azure = new CloudStorageAzureOptions
            {
                ConnectionString = "UseDevelopmentStorage=true"
            }
        };

        var extension = new CloudStorageOrmOptionsExtension(options);

        extension.Options.ShouldBeSameAs(options);
        extension.Info.ShouldNotBeNull();
    }

    [Fact]
    public void ApplyServices_RegistersCoreCloudStorageServices()
    {
        var services = new ServiceCollection();
        var extension = new CloudStorageOrmOptionsExtension(new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ContainerName = "unit",
            Azure = new CloudStorageAzureOptions
            {
                ConnectionString = "UseDevelopmentStorage=true"
            }
        });

        extension.ApplyServices(services);

        using var provider = services.BuildServiceProvider();
        provider.GetService<CloudStorageOptions>().ShouldNotBeNull();
        provider.GetService<IStorageProvider>().ShouldNotBeNull();
        provider.GetService<IBlobPathResolver>().ShouldNotBeNull();
    }

    [Fact]
    public void Validate_WithInvalidOptions_Throws()
    {
        var extension = new CloudStorageOrmOptionsExtension(new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ContainerName = "unit",
            Azure = new CloudStorageAzureOptions
            {
                ConnectionString = ""
            }
        });

        var ex = Should.Throw<InvalidOperationException>(() => extension.Validate(null!));
        ex.Message.ShouldContain("Azure.ConnectionString");
    }

    [Fact]
    public void Validate_WithValidAwsOptions_DoesNotThrow()
    {
        var extension = new CloudStorageOrmOptionsExtension(new CloudStorageOptions
        {
            Provider = CloudProvider.Aws,
            ContainerName = "unit-bucket",
            Aws = new CloudStorageAwsOptions
            {
                AccessKeyId = "test-key",
                SecretAccessKey = "test-secret",
                Region = "us-east-1"
            }
        });

        Should.NotThrow(() => extension.Validate(null!));
    }
}