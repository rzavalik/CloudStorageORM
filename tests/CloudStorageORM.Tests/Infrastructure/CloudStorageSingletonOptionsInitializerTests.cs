namespace CloudStorageORM.Tests.Infrastructure
{
    using CloudStorageORM.Infrastructure;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Moq;
    using Shouldly;
    using Xunit;

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
    }
}
