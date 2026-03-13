using CloudStorageORM.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

public class CloudStorageSingletonOptionsInitializerTests
{
    [Fact]
    public void Initialize_WithValidParameters_CallsCloudStorageSingletonOptionsInitialize()
    {
        var serviceProviderMock = new Mock<IServiceProvider>().Object;
        var optionsMock = new Mock<IDbContextOptions>().Object;
        var initializer = new CloudStorageSingletonOptionsInitializer();

        var exception = Record.Exception(() =>
            initializer.Initialize(serviceProviderMock, optionsMock)
        );

        exception.ShouldBeNull();
    }

    [Fact]
    public void EnsureInitialized_WithValidParameters_DoesNotThrow()
    {
        var serviceProvider = new Mock<IServiceProvider>().Object;
        var options = new Mock<IDbContextOptions>().Object;
        var initializer = new CloudStorageSingletonOptionsInitializer();

        Should.NotThrow(() => initializer.EnsureInitialized(serviceProvider, options));
    }

    [Fact]
    public void Validate_WithValidParameters_DoesNotThrow()
    {
        var options = new Mock<IDbContextOptions>().Object;
        var initializer = new CloudStorageSingletonOptionsInitializer();

        Should.NotThrow(() => initializer.Validate(options));
    }
}