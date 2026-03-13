namespace CloudStorageORM.Tests.Infrastructure
{
    using global::CloudStorageORM.Infrastructure;
    using Microsoft.EntityFrameworkCore;
    using Shouldly;

    public class CloudStorageDbSetInitializerTests
    {
        [Fact]
        public void InitializeSets_DoesNotThrow()
        {
            var sut = new CloudStorageDbSetInitializer();
            var options = new DbContextOptionsBuilder<MinimalDbContext>()
                .UseInMemoryDatabase("init-test")
                .Options;
            var ctx = new MinimalDbContext(options);

            Should.NotThrow(() => sut.InitializeSets(ctx));
        }
    }
}