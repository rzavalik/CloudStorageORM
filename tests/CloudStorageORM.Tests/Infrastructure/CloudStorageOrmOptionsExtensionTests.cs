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
            ConnectionString = "UseDevelopmentStorage=true",
            ContainerName = "unit"
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
            ConnectionString = "UseDevelopmentStorage=true",
            ContainerName = "unit"
        });

        extension.ApplyServices(services);

        using var provider = services.BuildServiceProvider();
        provider.GetService<CloudStorageOptions>().ShouldNotBeNull();
        provider.GetService<IStorageProvider>().ShouldNotBeNull();
        provider.GetService<IBlobPathResolver>().ShouldNotBeNull();
    }

    [Fact]
    public void Validate_DoesNotThrow()
    {
        var extension = new CloudStorageOrmOptionsExtension(new CloudStorageOptions());
        Should.NotThrow(() => extension.Validate(null!));
    }
}

