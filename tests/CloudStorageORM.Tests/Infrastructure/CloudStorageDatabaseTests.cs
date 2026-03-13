using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Moq;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

public class CloudStorageDatabaseTests
{
    [Fact]
    public async Task EnsureCreatedAsync_DelegatesToCreator()
    {
        var fixture = CreateFixture();
        fixture.CreatorMock.Setup(x => x.EnsureCreatedAsync(CancellationToken.None)).ReturnsAsync(true);

        await fixture.Database.EnsureCreatedAsync();

        fixture.CreatorMock.Verify(x => x.EnsureCreatedAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task EnsureDeletedAsync_DelegatesToCreator()
    {
        var fixture = CreateFixture();
        fixture.CreatorMock.Setup(x => x.EnsureDeletedAsync(CancellationToken.None)).ReturnsAsync(true);

        await fixture.Database.EnsureDeletedAsync();

        fixture.CreatorMock.Verify(x => x.EnsureDeletedAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_AddedEntry_SavesToStorage()
    {
        var fixture = CreateFixture();
        var entity = new DbUser { Id = "1", Name = "A" };
        fixture.Context.Add(entity);

        var entries = GetUpdateEntries(fixture.Context);
        fixture.PathResolverMock.Setup(x => x.GetPath(It.IsAny<IUpdateEntry>())).Returns("users/1.json");

        var changes = await fixture.Database.SaveChangesAsync(entries);

        changes.ShouldBe(1);
        fixture.StorageProviderMock.Verify(x => x.SaveAsync("users/1.json", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_ModifiedEntry_SavesToStorage()
    {
        var fixture = CreateFixture();
        var entity = new DbUser { Id = "2", Name = "Before" };
        fixture.Context.Attach(entity);
        entity.Name = "After";
        fixture.Context.Entry(entity).State = EntityState.Modified;

        var entries = GetUpdateEntries(fixture.Context);
        fixture.PathResolverMock.Setup(x => x.GetPath(It.IsAny<IUpdateEntry>())).Returns("users/2.json");

        var changes = await fixture.Database.SaveChangesAsync(entries);

        changes.ShouldBe(1);
        fixture.StorageProviderMock.Verify(x => x.SaveAsync("users/2.json", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_DeletedEntry_DeletesFromStorage()
    {
        var fixture = CreateFixture();
        var entity = new DbUser { Id = "3", Name = "ToDelete" };
        fixture.Context.Attach(entity);
        fixture.Context.Remove(entity);

        var entries = GetUpdateEntries(fixture.Context);
        fixture.PathResolverMock.Setup(x => x.GetPath(It.IsAny<IUpdateEntry>())).Returns("users/3.json");

        var changes = await fixture.Database.SaveChangesAsync(entries);

        changes.ShouldBe(1);
        fixture.StorageProviderMock.Verify(x => x.DeleteAsync("users/3.json"), Times.Once);
    }

    [Fact]
    public void SaveChanges_WhenEntryIsDetached_ReturnsZero()
    {
        var fixture = CreateFixture();
        var entity = new DbUser { Id = "4", Name = "Detached" };
        fixture.Context.Add(entity);
        IUpdateEntry entry = fixture.Context.Entry(entity).GetInfrastructure();
        fixture.Context.Entry(entity).State = EntityState.Detached;

        var changes = fixture.Database.SaveChanges([entry]);

        changes.ShouldBe(0);
    }

    [Fact]
    public async Task ToListAsync_SkipsNullReadEntities()
    {
        var fixture = CreateFixture();
        fixture.StorageProviderMock
            .Setup(x => x.ListAsync("users"))
            .ReturnsAsync(["users/1.json", "users/2.json"]);
        fixture.StorageProviderMock
            .Setup(x => x.ReadAsync<DbUser>("users/1.json"))
            .ReturnsAsync(new DbUser { Id = "1", Name = "One" });
        fixture.StorageProviderMock
            .Setup(x => x.ReadAsync<DbUser>("users/2.json"))
            .ReturnsAsync((DbUser)null!);

        var list = await fixture.Database.ToListAsync<DbUser>("users");

        list.Count.ShouldBe(1);
        list[0].Id.ShouldBe("1");
    }

    [Fact]
    public async Task TryLoadByPrimaryKeyAsync_WhenFound_ReturnsEntity()
    {
        var fixture = CreateFixture();
        fixture.PathResolverMock
            .Setup(x => x.GetPath(typeof(DbUser), "42"))
            .Returns("users/42.json");
        fixture.StorageProviderMock
            .Setup(x => x.ReadAsync<DbUser>("users/42.json"))
            .ReturnsAsync(new DbUser { Id = "42", Name = "Found" });

        var entity = await fixture.Database.TryLoadByPrimaryKeyAsync<DbUser>("42");

        entity.ShouldNotBeNull();
        entity.Name.ShouldBe("Found");
    }

    [Fact]
    public async Task TryLoadByPrimaryKeyAsync_WhenNotFound_ReturnsNull()
    {
        var fixture = CreateFixture();
        fixture.PathResolverMock
            .Setup(x => x.GetPath(typeof(DbUser), "404"))
            .Returns("users/404.json");
        fixture.StorageProviderMock
            .Setup(x => x.ReadAsync<DbUser>("users/404.json"))
            .ReturnsAsync((DbUser)null!);

        var entity = await fixture.Database.TryLoadByPrimaryKeyAsync<DbUser>("404");

        entity.ShouldBeNull();
    }

    [Fact]
    public async Task LoadEntitiesAsync_UsesBlobNameFromResolver()
    {
        var fixture = CreateFixture();
        fixture.PathResolverMock.Setup(x => x.GetBlobName(typeof(DbUser))).Returns("dbusers");
        fixture.StorageProviderMock.Setup(x => x.ListAsync("dbusers")).ReturnsAsync([]);

        var list = await fixture.Database.LoadEntitiesAsync<DbUser>(fixture.Context);

        list.ShouldNotBeNull();
        fixture.PathResolverMock.Verify(x => x.GetBlobName(typeof(DbUser)), Times.Once);
    }

    [Fact]
    public void CompileQueryExpression_ThrowsNotImplemented()
    {
        var fixture = CreateFixture();
        Should.Throw<NotImplementedException>(() =>
            fixture.Database.CompileQueryExpression<object>(Expression.Constant(1), false, new HashSet<string>()));
    }

    [Fact]
    public void CompileQuery_ForAsyncEnumerable_ReturnsCloudStorageQueryable()
    {
        var fixture = CreateFixture();
        var database = (IDatabase)fixture.Database;
        var compiled = database.CompileQuery<IAsyncEnumerable<DbUser>>(Expression.Constant(1), async: true);

        var result = compiled(null!);

        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<IAsyncEnumerable<DbUser>>();
    }

    [Fact]
    public void CompileQuery_ForSingleEntity_ExecutesProviderQueryWithoutCastError()
    {
        var fixture = CreateFixture();
        var database = (IDatabase)fixture.Database;

        fixture.PathResolverMock
            .Setup(x => x.GetPath(typeof(DbUser), "42"))
            .Returns("users/42.json");
        fixture.StorageProviderMock
            .Setup(x => x.ReadAsync<DbUser>("users/42.json"))
            .ReturnsAsync(new DbUser { Id = "42", Name = "Found" });

        var externalProvider = new CloudStorageQueryProvider(fixture.Database, fixture.PathResolverMock.Object);
        var queryable = new CloudStorageQueryable<DbUser>(externalProvider);
        var expression = BuildFirstOrDefaultByIdExpression(queryable, "42");

        var compiled = database.CompileQuery<DbUser?>(expression, async: false);
        var result = compiled(null!);

        result.ShouldNotBeNull();
        result.Id.ShouldBe("42");
        result.Name.ShouldBe("Found");
    }

    [Fact]
    public void CompileQuery_ForEnumerableWithEfQueryRoot_EnumeratesWithoutReducibleNodeError()
    {
        var fixture = CreateFixture();
        var database = (IDatabase)fixture.Database;

        fixture.PathResolverMock.Setup(x => x.GetBlobName(typeof(DbUser))).Returns("users");
        fixture.StorageProviderMock
            .Setup(x => x.ListAsync("users"))
            .ReturnsAsync(["users/1.json", "users/2.json"]);
        fixture.StorageProviderMock
            .Setup(x => x.ReadAsync<DbUser?>("users/1.json"))
            .ReturnsAsync(new DbUser { Id = "1", Name = "One" });
        fixture.StorageProviderMock
            .Setup(x => x.ReadAsync<DbUser?>("users/2.json"))
            .ReturnsAsync(new DbUser { Id = "2", Name = "Two" });

        var expression = fixture.Context.Users.Where(u => u.Name == "One").Expression;
        var compiled = database.CompileQuery<IEnumerable<DbUser>>(expression, async: false);

        var result = compiled(null!).ToList();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("One");
    }

    private static List<IUpdateEntry> GetUpdateEntries(DbContext context)
    {
        return context.ChangeTracker.Entries()
            .Select(e => (IUpdateEntry)e.GetInfrastructure())
            .ToList();
    }

    private static Expression BuildFirstOrDefaultByIdExpression(IQueryable<DbUser> source, string id)
    {
        Expression<Func<DbUser, bool>> predicate = user => user.Id == id;
        return Expression.Call(
            typeof(Queryable),
            nameof(Queryable.FirstOrDefault),
            [typeof(DbUser)],
            source.Expression,
            Expression.Quote(predicate));
    }

    private static DatabaseFixture CreateFixture()
    {
        var dbOptions = new DbContextOptionsBuilder<DatabaseTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DatabaseTestDbContext(dbOptions);

        var storageProviderMock = new Mock<IStorageProvider>();
        var pathResolverMock = new Mock<IBlobPathResolver>();
        var creatorMock = new Mock<IDatabaseCreator>();
        var strategyFactoryMock = new Mock<IExecutionStrategyFactory>();
        var currentDbContextMock = new Mock<ICurrentDbContext>();
        currentDbContextMock.SetupGet(x => x.Context).Returns(context);

        /*
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ConnectionString = "UseDevelopmentStorage=true",
            ContainerName = "tests"
        };
        */

        var database = new CloudStorageDatabase(
            context.Model,
            creatorMock.Object,
            strategyFactoryMock.Object,
            storageProviderMock.Object,
            currentDbContextMock.Object,
            pathResolverMock.Object);

        return new DatabaseFixture(database, context, storageProviderMock, pathResolverMock, creatorMock);
    }

    private sealed record DatabaseFixture(
        CloudStorageDatabase Database,
        DatabaseTestDbContext Context,
        Mock<IStorageProvider> StorageProviderMock,
        Mock<IBlobPathResolver> PathResolverMock,
        Mock<IDatabaseCreator> CreatorMock);
}

public class DatabaseTestDbContext(DbContextOptions<DatabaseTestDbContext> options) : DbContext(options)
{
    public DbSet<DbUser> Users => Set<DbUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbUser>().HasKey(x => x.Id);
    }
}

public class DbUser
{
    [MaxLength(100)]
    public string Id { get; init; } = string.Empty;
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}