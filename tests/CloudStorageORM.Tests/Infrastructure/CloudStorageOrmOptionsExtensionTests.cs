namespace CloudStorageORM.Tests.Infrastructure
{
    using Enums;
    using global::CloudStorageORM.Infrastructure;
    using Interfaces.Infrastructure;
    using Interfaces.StorageProviders;
    using Microsoft.Extensions.DependencyInjection;
    using Options;
    using Shouldly;

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
}

