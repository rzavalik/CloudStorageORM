﻿namespace CloudStorageORM.Tests.Extensions
{
    using CloudStorageORM.Enums;
    using CloudStorageORM.Extensions;
    using CloudStorageORM.Options;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Shouldly;
    using Xunit;

    public class CloudStorageORMServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddEntityFrameworkCloudStorageORM_WithNullServices_ThrowsArgumentNullException()
        {
            IServiceCollection services = null;
            var storageOptions = new CloudStorageOptions
            {
                Provider = CloudProvider.Azure,
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "test-container"
            };

            var exception = Should.Throw<ArgumentNullException>(() =>
                services.AddEntityFrameworkCloudStorageORM(storageOptions)
            );

            exception.ParamName.ShouldBe("services");
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

            services.AddEntityFrameworkCloudStorageORM(storageOptions);

            var provider = services.BuildServiceProvider();

            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseInternalServiceProvider(provider);

            var options = optionsBuilder.Options;

            options.Extensions.ShouldNotBeEmpty();
        }
    }
}
