using Azure.Storage.Blobs;
using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
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
            ConnectionString = "UseDevelopmentStorage=true",
            ContainerName = "test-container"
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
            ConnectionString = "UseDevelopmentStorage=true",
            ContainerName = "test-container"
        };

        services.AddEntityFrameworkCloudStorageOrm(storageOptions);

        var provider = services.BuildServiceProvider();

        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseInternalServiceProvider(provider);

        var options = optionsBuilder.Options;

        options.Extensions.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddEntityFrameworkCloudStorageORM_WithNullConnectionString_BlobServiceClientFactoryThrows()
    {
        var services = new ServiceCollection();
        var storageOptions = new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ConnectionString = null!,
            ContainerName = "test-container"
        };

        services.AddEntityFrameworkCloudStorageOrm(storageOptions);
        var provider = services.BuildServiceProvider();

        // Try to get BlobServiceClient which should trigger the factory validation
        var ex = Should.Throw<InvalidOperationException>(() =>
        {
            provider.GetService(typeof(BlobServiceClient));
        });

        ex.Message.ShouldContain("ConnectionString must be provided");
    }

    [Fact]
    public void AddEntityFrameworkCloudStorageORM_WithEmptyConnectionString_BlobServiceClientFactoryThrows()
    {
        var services = new ServiceCollection();
        var storageOptions = new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ConnectionString = "",
            ContainerName = "test-container"
        };

        services.AddEntityFrameworkCloudStorageOrm(storageOptions);
        var provider = services.BuildServiceProvider();

        // Try to get BlobServiceClient which should trigger the factory validation
        var ex = Should.Throw<InvalidOperationException>(() =>
        {
            provider.GetService(typeof(BlobServiceClient));
        });

        ex.Message.ShouldContain("ConnectionString must be provided");
    }
}