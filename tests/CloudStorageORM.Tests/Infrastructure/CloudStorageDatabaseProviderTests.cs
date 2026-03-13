using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

public class CloudStorageDatabaseProviderTests
{
    private readonly CloudStorageDatabaseProvider _sut = new();

    [Fact]
    public void Name_ReturnsExpectedValue()
        => _sut.Name.ShouldBe("CloudStorageORM.Provider");

    [Fact]
    public void IsConfigured_AlwaysReturnsTrue()
    {
        var options = new Mock<IDbContextOptions>().Object;
        _sut.IsConfigured(options).ShouldBeTrue();
    }

    [Fact]
    public void Create_WithValidDependencies_ReturnsCloudStorageDatabase()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Mock<IStorageProvider>().Object);
        services.AddSingleton(new Mock<IModel>().Object);
        services.AddSingleton<ICurrentDbContext>(_ =>
        {
            var context = new DbContext(new DbContextOptionsBuilder().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            var currentDbContext = new Mock<ICurrentDbContext>();
            currentDbContext.SetupGet(x => x.Context).Returns(context);
            return currentDbContext.Object;
        });
        services.AddSingleton(new Mock<IBlobPathResolver>().Object);

        var dependencies = new Mock<IDatabaseFacadeDependencies>();
        dependencies.SetupGet(x => x.DatabaseCreator).Returns(new Mock<IDatabaseCreator>().Object);
        dependencies.SetupGet(x => x.ExecutionStrategyFactory).Returns(new Mock<IExecutionStrategyFactory>().Object);
        dependencies.As<IInfrastructure<IServiceProvider>>().SetupGet(x => x.Instance)
            .Returns(services.BuildServiceProvider());

        var database = CloudStorageDatabaseProvider.Create(dependencies.Object);

        database.ShouldBeOfType<CloudStorageDatabase>();
    }

    [Fact]
    public void Create_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        var dependencies = new Mock<IDatabaseFacadeDependencies>();
        dependencies.SetupGet(x => x.DatabaseCreator).Returns(new Mock<IDatabaseCreator>().Object);
        dependencies.SetupGet(x => x.ExecutionStrategyFactory).Returns(new Mock<IExecutionStrategyFactory>().Object);
        dependencies.As<IInfrastructure<IServiceProvider>>().SetupGet(x => x.Instance)
            .Returns((IServiceProvider)null!);

        Should.Throw<ArgumentNullException>(() => CloudStorageDatabaseProvider.Create(dependencies.Object));
    }
}