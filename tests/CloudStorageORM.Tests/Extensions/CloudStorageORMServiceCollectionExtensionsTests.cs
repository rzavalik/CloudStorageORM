using Azure.Storage.Blobs;
using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CloudStorageORM.Tests.Extensions;

public class CloudStorageOrmServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEntityFrameworkCloudStorageORM_WithNullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        var storageOptions = new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ContainerName = "test-container",
            Azure = new CloudStorageAzureOptions
            {
                ConnectionString = "UseDevelopmentStorage=true"
            }
        };

        var exception = Should.Throw<ArgumentNullException>(() =>
            services!.AddEntityFrameworkCloudStorageOrm(storageOptions)
        );

        exception.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEntityFrameworkCloudStorageORM_WithNullOptions_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        CloudStorageOptions? storageOptions = null;

        var exception = Should.Throw<ArgumentNullException>(() =>
            services.AddEntityFrameworkCloudStorageOrm(storageOptions!)
        );

        exception.ParamName.ShouldBe("storageOptions");
    }

    [Fact]
    public void AddEntityFrameworkCloudStorageORM_WithValidServices_RegistersDbContextOptionsExtension()
    {
        var services = new ServiceCollection();
        var storageOptions = new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ContainerName = "test-container",
            Azure = new CloudStorageAzureOptions
            {
                ConnectionString = "UseDevelopmentStorage=true"
            }
        };

        services.AddEntityFrameworkCloudStorageOrm(storageOptions);

        var provider = services.BuildServiceProvider();

        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseInternalServiceProvider(provider);

        var options = optionsBuilder.Options;

        options.Extensions.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddEntityFrameworkCloudStorageORM_WithNullConnectionString_ThrowsDuringValidation()
    {
        var services = new ServiceCollection();
        var storageOptions = new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ContainerName = "test-container",
            Azure = new CloudStorageAzureOptions
            {
                ConnectionString = null!
            }
        };

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddEntityFrameworkCloudStorageOrm(storageOptions));

        ex.Message.ShouldContain("Azure.ConnectionString");
    }

    [Fact]
    public void AddEntityFrameworkCloudStorageORM_WithEmptyConnectionString_ThrowsDuringValidation()
    {
        var services = new ServiceCollection();
        var storageOptions = new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ContainerName = "test-container",
            Azure = new CloudStorageAzureOptions
            {
                ConnectionString = ""
            }
        };

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddEntityFrameworkCloudStorageOrm(storageOptions));

        ex.Message.ShouldContain("Azure.ConnectionString");
    }

    [Fact]
    public void AddEntityFrameworkCloudStorageORM_WithValidAwsOptions_DoesNotRegisterBlobServiceClient()
    {
        var services = new ServiceCollection();
        var storageOptions = new CloudStorageOptions
        {
            Provider = CloudProvider.Aws,
            ContainerName = "test-bucket",
            Aws = new CloudStorageAwsOptions
            {
                AccessKeyId = "test-key",
                SecretAccessKey = "test-secret",
                Region = "us-east-1"
            }
        };

        services.AddEntityFrameworkCloudStorageOrm(storageOptions);
        using var provider = services.BuildServiceProvider();

        provider.GetService<BlobServiceClient>().ShouldBeNull();
        provider.GetService<CloudStorageOptions>().ShouldNotBeNull();
        provider.GetService(typeof(IStorageProvider)).ShouldNotBeNull();
    }

    [Fact]
    public void AddEntityFrameworkCloudStorageORM_WithMissingAwsRegion_ThrowsDuringValidation()
    {
        var services = new ServiceCollection();
        var storageOptions = new CloudStorageOptions
        {
            Provider = CloudProvider.Aws,
            ContainerName = "test-bucket",
            Aws = new CloudStorageAwsOptions
            {
                AccessKeyId = "test-key",
                SecretAccessKey = "test-secret",
                Region = ""
            }
        };

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddEntityFrameworkCloudStorageOrm(storageOptions));

        ex.Message.ShouldContain("Aws.Region");
    }
}