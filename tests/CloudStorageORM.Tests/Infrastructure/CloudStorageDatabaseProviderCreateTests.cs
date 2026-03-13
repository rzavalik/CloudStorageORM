namespace CloudStorageORM.Tests.Infrastructure
{
    using Enums;
    using global::CloudStorageORM.Infrastructure;
    using Interfaces.Infrastructure;
    using Interfaces.StorageProviders;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Options;
    using Shouldly;

    public class CloudStorageDatabaseProviderCreateTests
    {
        [Fact]
        public void Create_ReturnsCloudStorageDatabase()
        {
            var services = new ServiceCollection();
            var dbOptions = new DbContextOptionsBuilder<ProviderDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ProviderDbContext(dbOptions);

            var currentDbContext = new Mock<ICurrentDbContext>();
            currentDbContext.SetupGet(x => x.Context).Returns(context);

            var storage = new Mock<IStorageProvider>().Object;
            services.AddSingleton(storage);
            services.AddSingleton(new CloudStorageOptions { Provider = CloudProvider.Azure });
            services.AddSingleton(context.Model);
            services.AddSingleton(currentDbContext.Object);
            services.AddSingleton(Mock.Of<IBlobPathResolver>());
            using var serviceProvider = services.BuildServiceProvider();

            var dependencies = new Mock<IDatabaseFacadeDependencies>();
            dependencies.SetupGet(x => x.DatabaseCreator).Returns(Mock.Of<IDatabaseCreator>());
            dependencies.SetupGet(x => x.ExecutionStrategyFactory).Returns(Mock.Of<IExecutionStrategyFactory>());
            dependencies.SetupGet(x => x.ConcurrencyDetector).Returns(Mock.Of<IConcurrencyDetector>());
            dependencies.As<IInfrastructure<IServiceProvider>>()
                .Setup(x => x.Instance)
                .Returns(serviceProvider);

            var result = CloudStorageDatabaseProvider.Create(dependencies.Object);

            result.ShouldNotBeNull();
            result.ShouldBeOfType<CloudStorageDatabase>();
        }

        private sealed class ProviderDbContext(DbContextOptions<ProviderDbContext> options) : DbContext(options);
    }
}