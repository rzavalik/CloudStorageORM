using CloudStorageORM.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

public class CloudStorageQueryContextFactoryTests
{
    [Fact]
    public void Create_ReturnsCloudStorageQueryContext()
    {
        var options = new DbContextOptionsBuilder<FactoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new FactoryDbContext(options);
        var deps = context.GetService<QueryContextDependencies>();

        var factory = new CloudStorageQueryContextFactory(deps);
        var ctx = factory.Create();

        ctx.ShouldNotBeNull();
        ctx.ShouldBeOfType<CloudStorageQueryContext>();
    }

    private sealed class FactoryDbContext(DbContextOptions<FactoryDbContext> options) : DbContext(options);
}