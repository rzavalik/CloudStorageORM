using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using CloudStorageORM.Abstractions;
using CloudStorageORM.Extensions;
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
    public async Task SaveChangesAsync_ModifiedEntry_WithEtagConcurrency_UsesConditionalSaveAndRefreshesTrackedValues()
    {
        var fixture = CreateEtagFixture();
        var entity = new EtagDbUser { Id = "7", Name = "Before" };
        fixture.Context.Attach(entity);
        fixture.Context.Entry(entity).Property("ETag").OriginalValue = "etag-1";
        fixture.Context.Entry(entity).Property("ETag").CurrentValue = "etag-1";
        entity.Name = "After";
        fixture.Context.Entry(entity).State = EntityState.Modified;

        var entries = GetUpdateEntries(fixture.Context);
        fixture.PathResolverMock.Setup(x => x.GetPath(It.IsAny<IUpdateEntry>())).Returns("users/7.json");
        fixture.StorageProviderMock
            .Setup(x => x.SaveAsync("users/7.json", It.IsAny<object>(), "etag-1"))
            .ReturnsAsync("etag-2");

        var changes = await fixture.Database.SaveChangesAsync(entries);

        changes.ShouldBe(1);
        fixture.StorageProviderMock.Verify(x => x.SaveAsync("users/7.json", It.IsAny<object>(), "etag-1"), Times.Once);
        fixture.Context.Entry(entity).Property("ETag").OriginalValue.ShouldBe("etag-2");
        entity.ETag.ShouldBe("etag-2");
    }

    [Fact]
    public async Task SaveChangesAsync_ModifiedEntry_WithEtagConcurrencyConflict_ThrowsDbUpdateConcurrencyException()
    {
        var fixture = CreateEtagFixture();
        var entity = new EtagDbUser { Id = "8", Name = "Before" };
        fixture.Context.Attach(entity);
        fixture.Context.Entry(entity).Property("ETag").OriginalValue = "etag-1";
        fixture.Context.Entry(entity).Property("ETag").CurrentValue = "etag-1";
        entity.Name = "After";
        fixture.Context.Entry(entity).State = EntityState.Modified;

        var entries = GetUpdateEntries(fixture.Context);
        fixture.PathResolverMock.Setup(x => x.GetPath(It.IsAny<IUpdateEntry>())).Returns("users/8.json");
        fixture.StorageProviderMock
            .Setup(x => x.SaveAsync("users/8.json", It.IsAny<object>(), "etag-1"))
            .ThrowsAsync(new StoragePreconditionFailedException("users/8.json"));

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => fixture.Database.SaveChangesAsync(entries));
    }

    [Fact]
    public async Task SaveChangesAsync_InsideTransactionThenRollback_DoesNotPersist()
    {
        var fixture = CreateFixture();
        var entity = new DbUser { Id = "tx-rollback", Name = "Rollback" };
        fixture.Context.Add(entity);

        var entries = GetUpdateEntries(fixture.Context);
        fixture.PathResolverMock.Setup(x => x.GetPath(It.IsAny<IUpdateEntry>())).Returns("users/tx-rollback.json");

        await fixture.TransactionManager.BeginTransactionAsync();
        var changes = await fixture.Database.SaveChangesAsync(entries);
        await fixture.TransactionManager.RollbackTransactionAsync();

        changes.ShouldBe(1);
        fixture.StorageProviderMock.Verify(x => x.SaveAsync("users/tx-rollback.json", It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsync_InsideTransactionThenCommit_Persists()
    {
        var fixture = CreateFixture();
        var entity = new DbUser { Id = "tx-commit", Name = "Commit" };
        fixture.Context.Add(entity);

        var entries = GetUpdateEntries(fixture.Context);
        fixture.PathResolverMock.Setup(x => x.GetPath(It.IsAny<IUpdateEntry>())).Returns("users/tx-commit.json");

        await fixture.TransactionManager.BeginTransactionAsync();
        var changes = await fixture.Database.SaveChangesAsync(entries);
        fixture.StorageProviderMock.Verify(x => x.SaveAsync("users/tx-commit.json", It.IsAny<object>()), Times.Never);

        await fixture.TransactionManager.CommitTransactionAsync();

        changes.ShouldBe(1);
        fixture.StorageProviderMock.Verify(x => x.SaveAsync("users/tx-commit.json", It.IsAny<object>()), Times.Once);
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
            .Setup(x => x.ReadWithMetadataAsync<DbUser>("users/1.json"))
            .ReturnsAsync(new StorageObject<DbUser>(new DbUser { Id = "1", Name = "One" }, "etag-1", true));
        fixture.StorageProviderMock
            .Setup(x => x.ReadWithMetadataAsync<DbUser>("users/2.json"))
            .ReturnsAsync(new StorageObject<DbUser>(null, null, false));

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
            .Setup(x => x.ReadWithMetadataAsync<DbUser>("users/42.json"))
            .ReturnsAsync(new StorageObject<DbUser>(new DbUser { Id = "42", Name = "Found" }, "etag-42", true));

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
            .Setup(x => x.ReadWithMetadataAsync<DbUser>("users/404.json"))
            .ReturnsAsync(new StorageObject<DbUser>(null, null, false));

        var entity = await fixture.Database.TryLoadByPrimaryKeyAsync<DbUser>("404");

        entity.ShouldBeNull();
    }

    [Fact]
    public async Task TryLoadByPrimaryKeyAsync_WithEtagConcurrency_PopulatesTrackedEtagAndIETag()
    {
        var fixture = CreateEtagFixture();
        fixture.PathResolverMock
            .Setup(x => x.GetPath(typeof(EtagDbUser), "9"))
            .Returns("users/9.json");
        fixture.StorageProviderMock
            .Setup(x => x.ReadWithMetadataAsync<EtagDbUser>("users/9.json"))
            .ReturnsAsync(new StorageObject<EtagDbUser>(new EtagDbUser { Id = "9", Name = "Tracked" }, "etag-9", true));

        var entity = await fixture.Database.TryLoadByPrimaryKeyAsync<EtagDbUser>("9", fixture.Context);

        entity.ShouldNotBeNull();
        entity.ETag.ShouldBe("etag-9");
        fixture.Context.Entry(entity).Property("ETag").OriginalValue.ShouldBe("etag-9");
    }

    [Fact]
    public async Task LoadEntitiesAsync_UsesBlobNameFromResolver()
    {
        const string dbUsers = "dbusers";
        var fixture = CreateFixture();
        fixture.PathResolverMock.Setup(x => x.GetBlobName(typeof(DbUser))).Returns(dbUsers);
        fixture.StorageProviderMock.Setup(x => x.ListAsync(dbUsers)).ReturnsAsync([]);

        var list = await fixture.Database.LoadEntitiesAsync<DbUser>(fixture.Context);

        list.ShouldNotBeNull();
        fixture.PathResolverMock.Verify(x => x.GetBlobName(typeof(DbUser)), Times.Once);
    }

    [Fact]
    public async Task LoadPageAsync_UsesPagedListingAndLoadsOnlyRequestedWindow()
    {
        var fixture = CreateFixture();
        fixture.PathResolverMock.Setup(x => x.GetBlobName(typeof(DbUser))).Returns("users");
        fixture.StorageProviderMock
            .Setup(x => x.ListPageAsync("users", It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StorageListPage(["users/1.json", "users/2.json", "users/3.json"], null, false));
        fixture.StorageProviderMock
            .Setup(x => x.ReadWithMetadataAsync<DbUser?>("users/2.json"))
            .ReturnsAsync(new StorageObject<DbUser?>(new DbUser { Id = "2", Name = "Two" }, "etag-2", true));

        var page = await fixture.Database.LoadPageAsync<DbUser>(skip: 1, take: 1, fixture.Context);

        page.Count.ShouldBe(1);
        page[0].Id.ShouldBe("2");
        fixture.StorageProviderMock.Verify(x => x.ReadWithMetadataAsync<DbUser?>("users/1.json"), Times.Never);
        fixture.StorageProviderMock.Verify(x => x.ReadWithMetadataAsync<DbUser?>("users/2.json"), Times.Once);
        fixture.StorageProviderMock.Verify(x => x.ReadWithMetadataAsync<DbUser?>("users/3.json"), Times.Never);
    }

    [Fact]
    public async Task LoadPageAsync_TakeZero_ReturnsEmptyWithoutProviderCalls()
    {
        var fixture = CreateFixture();

        var page = await fixture.Database.LoadPageAsync<DbUser>(skip: 10, take: 0, fixture.Context);

        page.ShouldBeEmpty();
        fixture.StorageProviderMock.Verify(
            x => x.ListPageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadPageAsync_WhenFirstPageIsEmptyAndTerminal_ReturnsEmpty()
    {
        var fixture = CreateFixture();
        fixture.PathResolverMock.Setup(x => x.GetBlobName(typeof(DbUser))).Returns("users");
        fixture.StorageProviderMock
            .Setup(x => x.ListPageAsync("users", It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StorageListPage([], null, false));

        var page = await fixture.Database.LoadPageAsync<DbUser>(skip: 0, take: 5, fixture.Context);

        page.ShouldBeEmpty();
        fixture.StorageProviderMock.Verify(x => x.ReadWithMetadataAsync<DbUser?>(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadPageAsync_WhenContinuationExists_RequestsNextPage()
    {
        var fixture = CreateFixture();
        fixture.PathResolverMock.Setup(x => x.GetBlobName(typeof(DbUser))).Returns("users");
        fixture.StorageProviderMock
            .SetupSequence(x =>
                x.ListPageAsync("users", It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StorageListPage(["users/1.json"], "next", true))
            .ReturnsAsync(new StorageListPage(["users/2.json"], null, false));
        fixture.StorageProviderMock
            .Setup(x => x.ReadWithMetadataAsync<DbUser?>("users/1.json"))
            .ReturnsAsync(new StorageObject<DbUser?>(new DbUser { Id = "1", Name = "One" }, "etag-1", true));
        fixture.StorageProviderMock
            .Setup(x => x.ReadWithMetadataAsync<DbUser?>("users/2.json"))
            .ReturnsAsync(new StorageObject<DbUser?>(new DbUser { Id = "2", Name = "Two" }, "etag-2", true));

        var page = await fixture.Database.LoadPageAsync<DbUser>(skip: 1, take: 1, fixture.Context);

        page.Count.ShouldBe(1);
        page[0].Id.ShouldBe("2");
        fixture.StorageProviderMock.Verify(
            x => x.ListPageAsync("users", It.IsAny<int>(), null, It.IsAny<CancellationToken>()), Times.Once);
        fixture.StorageProviderMock.Verify(
            x => x.ListPageAsync("users", It.IsAny<int>(), "next", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadPageAsync_WhenContinuationTokenIsWhitespace_StopsPaging()
    {
        var fixture = CreateFixture();
        fixture.PathResolverMock.Setup(x => x.GetBlobName(typeof(DbUser))).Returns("users");
        fixture.StorageProviderMock
            .Setup(x => x.ListPageAsync("users", It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StorageListPage(["users/1.json"], "   ", true));
        fixture.StorageProviderMock
            .Setup(x => x.ReadWithMetadataAsync<DbUser?>("users/1.json"))
            .ReturnsAsync(new StorageObject<DbUser?>(new DbUser { Id = "1", Name = "One" }, "etag-1", true));

        var page = await fixture.Database.LoadPageAsync<DbUser>(skip: 0, take: 2, fixture.Context);

        page.Count.ShouldBe(1);
        fixture.StorageProviderMock.Verify(
            x => x.ListPageAsync("users", It.IsAny<int>(), null, It.IsAny<CancellationToken>()), Times.Once);
        fixture.StorageProviderMock.Verify(
            x => x.ListPageAsync("users", It.IsAny<int>(), It.Is<string?>(s => s == "   "),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadByPrimaryKeyRangePageAsync_TakeZero_ReturnsEmptyWithoutProviderCalls()
    {
        var fixture = CreateFixture();

        var page = await fixture.Database.LoadByPrimaryKeyRangePageAsync<DbUser>(
            lowerBound: null,
            lowerInclusive: true,
            upperBound: null,
            upperInclusive: true,
            skip: 0,
            take: 0,
            context: fixture.Context);

        page.ShouldBeEmpty();
        fixture.StorageProviderMock.Verify(
            x => x.ListPageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadByPrimaryKeyRangePageAsync_UsesPagedListingAndAppliesRangeSkipTake()
    {
        var fixture = CreateFixture();
        fixture.PathResolverMock.Setup(x => x.GetBlobName(typeof(DbUser))).Returns("users");
        fixture.StorageProviderMock
            .Setup(x => x.ListPageAsync("users", It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StorageListPage(["users/1.json", "users/2.json", "users/3.json"], null, false));
        fixture.StorageProviderMock
            .Setup(x => x.ReadWithMetadataAsync<DbUser?>("users/2.json"))
            .ReturnsAsync(new StorageObject<DbUser?>(new DbUser { Id = "2", Name = "Two" }, "etag-2", true));

        var page = await fixture.Database.LoadByPrimaryKeyRangePageAsync<DbUser>(
            lowerBound: "1",
            lowerInclusive: true,
            upperBound: "3",
            upperInclusive: false,
            skip: 1,
            take: 1,
            context: fixture.Context);

        page.Count.ShouldBe(1);
        page[0].Id.ShouldBe("2");
        fixture.StorageProviderMock.Verify(x => x.ReadWithMetadataAsync<DbUser?>("users/1.json"), Times.Never);
        fixture.StorageProviderMock.Verify(x => x.ReadWithMetadataAsync<DbUser?>("users/2.json"), Times.Once);
        fixture.StorageProviderMock.Verify(x => x.ReadWithMetadataAsync<DbUser?>("users/3.json"), Times.Never);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, -1)]
    public async Task LoadPageAsync_WithInvalidPagination_Throws(int skip, int take)
    {
        var fixture = CreateFixture();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => fixture.Database.LoadPageAsync<DbUser>(skip, take));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, -1)]
    public async Task LoadByPrimaryKeyRangePageAsync_WithInvalidPagination_Throws(int skip, int take)
    {
        var fixture = CreateFixture();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            fixture.Database.LoadByPrimaryKeyRangePageAsync<DbUser>(
                lowerBound: null,
                lowerInclusive: true,
                upperBound: null,
                upperInclusive: true,
                skip: skip,
                take: take,
                context: fixture.Context));
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
            .Setup(x => x.ReadWithMetadataAsync<DbUser>("users/42.json"))
            .ReturnsAsync(new StorageObject<DbUser>(new DbUser { Id = "42", Name = "Found" }, "etag-42", true));

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
            .Setup(x => x.ReadWithMetadataAsync<DbUser?>("users/1.json"))
            .ReturnsAsync(new StorageObject<DbUser?>(new DbUser { Id = "1", Name = "One" }, "etag-1", true));
        fixture.StorageProviderMock
            .Setup(x => x.ReadWithMetadataAsync<DbUser?>("users/2.json"))
            .ReturnsAsync(new StorageObject<DbUser?>(new DbUser { Id = "2", Name = "Two" }, "etag-2", true));

        var expression = fixture.Context.Users.Where(u => u.Name == "One").Expression;
        var compiled = database.CompileQuery<IEnumerable<DbUser>>(expression, async: false);

        var result = compiled(null!).ToList();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("One");
    }

    private static List<IUpdateEntry> GetUpdateEntries(DbContext context)
    {
        return context.ChangeTracker.Entries()
            .Select(IUpdateEntry (e) => e.GetInfrastructure())
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
        var transactionManager = new CloudStorageTransactionManager();
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
            pathResolverMock.Object,
            transactionManager);

        return new DatabaseFixture(
            database,
            context,
            storageProviderMock,
            pathResolverMock,
            creatorMock,
            transactionManager);
    }

    private static EtagDatabaseFixture CreateEtagFixture()
    {
        var dbOptions = new DbContextOptionsBuilder<EtagDatabaseTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new EtagDatabaseTestDbContext(dbOptions);

        var storageProviderMock = new Mock<IStorageProvider>();
        var pathResolverMock = new Mock<IBlobPathResolver>();
        var creatorMock = new Mock<IDatabaseCreator>();
        var strategyFactoryMock = new Mock<IExecutionStrategyFactory>();
        var currentDbContextMock = new Mock<ICurrentDbContext>();
        var transactionManager = new CloudStorageTransactionManager();
        currentDbContextMock.SetupGet(x => x.Context).Returns(context);

        var database = new CloudStorageDatabase(
            context.Model,
            creatorMock.Object,
            strategyFactoryMock.Object,
            storageProviderMock.Object,
            currentDbContextMock.Object,
            pathResolverMock.Object,
            transactionManager);

        return new EtagDatabaseFixture(
            database,
            context,
            storageProviderMock,
            pathResolverMock);
    }

    private sealed record DatabaseFixture(
        CloudStorageDatabase Database,
        DatabaseTestDbContext Context,
        Mock<IStorageProvider> StorageProviderMock,
        Mock<IBlobPathResolver> PathResolverMock,
        Mock<IDatabaseCreator> CreatorMock,
        CloudStorageTransactionManager TransactionManager);

    private sealed record EtagDatabaseFixture(
        CloudStorageDatabase Database,
        EtagDatabaseTestDbContext Context,
        Mock<IStorageProvider> StorageProviderMock,
        Mock<IBlobPathResolver> PathResolverMock);
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
    [MaxLength(100)] public string Id { get; init; } = string.Empty;
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
}

public sealed class EtagDatabaseTestDbContext(DbContextOptions<EtagDatabaseTestDbContext> options) : DbContext(options)
{
    public DbSet<EtagDbUser> Users => Set<EtagDbUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EtagDbUser>().HasKey(x => x.Id);
        modelBuilder.Entity<EtagDbUser>().UseObjectETagConcurrency();
    }
}

public sealed class EtagDbUser : IETag
{
    [MaxLength(100)] public string Id { get; init; } = string.Empty;

    [MaxLength(100)] public string Name { get; set; } = string.Empty;

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string? ETag { get; set; }
}