using System.Collections;
using System.ComponentModel.DataAnnotations;
using CloudStorageORM.Abstractions;
using CloudStorageORM.Enums;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

/// <summary>
/// Tests verifying that LINQ expressions are resolved directly against blob storage
/// rather than requiring a full materialization (ToList) first.
/// </summary>
public class CloudStorageQueryProviderTests
{
    private const string UserId1 = "user-1";
    private const string UserId2 = "user-2";

    private static (CloudStorageQueryProvider provider, Mock<IStorageProvider> storageProviderMock)
        BuildProvider(List<QueryTestUser>? seed = null)
    {
        seed ??= new List<QueryTestUser>();
        var storageProviderMock = new Mock<IStorageProvider>();
        storageProviderMock.Setup(x => x.CloudProvider).Returns(CloudProvider.Azure);
        storageProviderMock.Setup(x => x.SanitizeBlobName(It.IsAny<string>()))
            .Returns<string>(s => s);

        // BlobPathResolver uses SanitizeBlobName, so a real instance is fine.
        var pathResolver = new BlobPathResolver(storageProviderMock.Object);
        var blobName = pathResolver.GetBlobName(typeof(QueryTestUser));

        // ListAsync returns the blob paths for all seeded entities.
        storageProviderMock
            .Setup(x => x.ListAsync(blobName))
            .ReturnsAsync(seed.Select(u => $"{blobName}/{u.Id}.json").ToList());
        SetupPagedListing(
            storageProviderMock,
            seed.Select(u => $"{blobName}/{u.Id}.json").ToList());

        // ReadAsync returns the correct entity for each path.
        foreach (var user in seed)
        {
            var path = $"{blobName}/{user.Id}.json";
            storageProviderMock
                .Setup(x => x.ReadWithMetadataAsync<QueryTestUser>(path))
                .ReturnsAsync(new StorageObject<QueryTestUser>(user, "etag", true));
            storageProviderMock
                .Setup(x => x.ReadWithMetadataAsync<QueryTestUser?>(path))
                .ReturnsAsync(new StorageObject<QueryTestUser?>(user, "etag", true));
        }

        var database = BuildDatabase(storageProviderMock.Object);
        var provider = new CloudStorageQueryProvider(database, pathResolver);
        return (provider, storageProviderMock);
    }

    private static (CloudStorageQueryProvider provider, Mock<IStorageProvider> storageProviderMock)
        BuildRangeProvider(List<RangeQueryTestUser>? seed = null)
    {
        seed ??= [];
        var storageProviderMock = new Mock<IStorageProvider>();
        storageProviderMock.Setup(x => x.CloudProvider).Returns(CloudProvider.Azure);
        storageProviderMock.Setup(x => x.SanitizeBlobName(It.IsAny<string>()))
            .Returns<string>(s => s);

        var pathResolver = new BlobPathResolver(storageProviderMock.Object);
        var blobName = pathResolver.GetBlobName(typeof(RangeQueryTestUser));

        storageProviderMock
            .Setup(x => x.ListAsync(blobName))
            .ReturnsAsync(seed.Select(u => $"{blobName}/{u.Id}.json").ToList());
        SetupPagedListing(
            storageProviderMock,
            seed.Select(u => $"{blobName}/{u.Id}.json").ToList());

        foreach (var user in seed)
        {
            var path = $"{blobName}/{user.Id}.json";
            storageProviderMock
                .Setup(x => x.ReadWithMetadataAsync<RangeQueryTestUser>(path))
                .ReturnsAsync(new StorageObject<RangeQueryTestUser>(user, "etag", true));
            storageProviderMock
                .Setup(x => x.ReadWithMetadataAsync<RangeQueryTestUser?>(path))
                .ReturnsAsync(new StorageObject<RangeQueryTestUser?>(user, "etag", true));
        }

        var database = BuildRangeDatabase(storageProviderMock.Object);
        var provider = new CloudStorageQueryProvider(database, pathResolver);
        return (provider, storageProviderMock);
    }

    private static CloudStorageDatabase BuildDatabase(IStorageProvider storageProvider)
    {
        var options = new DbContextOptionsBuilder<MinimalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new MinimalDbContext(options);

        var modelMock = new Mock<IModel>();
        var entityTypeMock = new Mock<IEntityType>();
        var keyMock = new Mock<IKey>();
        var propMock = new Mock<IProperty>();

        propMock.Setup(p => p.Name).Returns(nameof(QueryTestUser.Id));
        propMock.Setup(p => p.PropertyInfo)
            .Returns(typeof(QueryTestUser).GetProperty(nameof(QueryTestUser.Id))!);
        keyMock.Setup(k => k.Properties)
            .Returns(new[] { propMock.Object }
                .ToList()
                .AsReadOnly()
                .ToList()
                .AsReadOnly());
        entityTypeMock.Setup(e => e.ClrType).Returns(typeof(QueryTestUser));
        entityTypeMock.Setup(e => e.FindPrimaryKey()).Returns(keyMock.Object);
        modelMock.Setup(m => m.FindEntityType(typeof(QueryTestUser))).Returns(entityTypeMock.Object);

        var currentDbContextMock = new Mock<ICurrentDbContext>();
        currentDbContextMock.Setup(c => c.Context).Returns(context);

        var creatorMock = new Mock<IDatabaseCreator>();
        var strategyFactoryMock = new Mock<IExecutionStrategyFactory>();

        /*
        var cloudOptions = new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ConnectionString = "UseDevelopmentStorage=true",
            ContainerName = "test"
        };
        */

        return new CloudStorageDatabase(
            modelMock.Object,
            creatorMock.Object,
            strategyFactoryMock.Object,
            storageProvider,
            currentDbContextMock.Object,
            new BlobPathResolver(storageProvider),
            new CloudStorageTransactionManager());
    }

    private static CloudStorageDatabase BuildRangeDatabase(IStorageProvider storageProvider)
    {
        var options = new DbContextOptionsBuilder<RangeMinimalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new RangeMinimalDbContext(options);

        var modelMock = new Mock<IModel>();
        var entityTypeMock = new Mock<IEntityType>();
        var keyMock = new Mock<IKey>();
        var propMock = new Mock<IProperty>();

        propMock.Setup(p => p.Name).Returns(nameof(RangeQueryTestUser.Id));
        propMock.Setup(p => p.PropertyInfo)
            .Returns(typeof(RangeQueryTestUser).GetProperty(nameof(RangeQueryTestUser.Id))!);
        propMock.Setup(p => p.ClrType).Returns(typeof(int));
        keyMock.Setup(k => k.Properties)
            .Returns(new[] { propMock.Object }
                .ToList()
                .AsReadOnly()
                .ToList()
                .AsReadOnly());
        entityTypeMock.Setup(e => e.ClrType).Returns(typeof(RangeQueryTestUser));
        entityTypeMock.Setup(e => e.FindPrimaryKey()).Returns(keyMock.Object);
        modelMock.Setup(m => m.FindEntityType(typeof(RangeQueryTestUser))).Returns(entityTypeMock.Object);

        var currentDbContextMock = new Mock<ICurrentDbContext>();
        currentDbContextMock.Setup(c => c.Context).Returns(context);

        var creatorMock = new Mock<IDatabaseCreator>();
        var strategyFactoryMock = new Mock<IExecutionStrategyFactory>();

        return new CloudStorageDatabase(
            modelMock.Object,
            creatorMock.Object,
            strategyFactoryMock.Object,
            storageProvider,
            currentDbContextMock.Object,
            new BlobPathResolver(storageProvider),
            new CloudStorageTransactionManager());
    }

    [Fact]
    public void FirstOrDefault_WithPrimaryKeyPredicate_LoadsSingleBlobDirectly()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" },
            new() { Id = UserId2, Name = "Bob" }
        };

        var (provider, storageMock) = BuildProvider(seed);
        var pathResolver = new BlobPathResolver(storageMock.Object);
        var blobName = pathResolver.GetBlobName(typeof(QueryTestUser));

        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);
        var result = queryable.FirstOrDefault(u => u.Id == UserId1);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Alice");

        // ListAsync should NOT have been called – we resolved directly via key path.
        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.Never);
        storageMock.Verify(x => x.ReadWithMetadataAsync<QueryTestUser>($"{blobName}/{UserId1}.json"), Times.Once);
    }

    [Fact]
    public void FirstOrDefault_WithNonKeyPredicate_FiltersInMemoryAfterFullLoad()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" },
            new() { Id = UserId2, Name = "Bob" }
        };

        var (provider, storageMock) = BuildProvider(seed);

        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);
        var result = queryable.FirstOrDefault(u => u.Name == "Bob");

        result.ShouldNotBeNull();
        result.Id.ShouldBe(UserId2);

        // Full list load must have been used because Name is not the PK.
        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Where_ReturnsFilteredResults_WithoutRequiringToList()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" },
            new() { Id = UserId2, Name = "Bob" }
        };

        var (provider, _) = BuildProvider(seed);

        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);
        var results = ((IQueryable<QueryTestUser>)queryable).Where(u => u.Name == "Alice").ToList();

        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(UserId1);
    }

    [Fact]
    public void FirstOrDefault_WithPrimaryKey_ReturnsNull_WhenNotFound()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" }
        };

        var (provider, storageMock) = BuildProvider(seed);
        var pathResolver = new BlobPathResolver(storageMock.Object);
        var blobName = pathResolver.GetBlobName(typeof(QueryTestUser));

        // blob for a missing key returns null
        storageProviderMock_SetupMissingRead(storageMock, $"{blobName}/missing-id.json");

        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);
        var result = queryable.FirstOrDefault(u => u.Id == "missing-id");

        result.ShouldBeNull();
        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Any_ExecutesViaFullLoad_WhenNoPrimaryKeyFastPath()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" }
        };

        var (provider, storageMock) = BuildProvider(seed);

        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);
        var exists = queryable.Any(u => u.Name == "Alice");

        exists.ShouldBeTrue();
        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void FirstOrDefault_WithoutPredicate_UsesFullLoadPath()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" }
        };

        var (provider, storageMock) = BuildProvider(seed);
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);

        var result = queryable.FirstOrDefault();

        result.ShouldNotBeNull();
        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void WhereThenFirstOrDefault_WithPrimaryKey_UsesDirectLookupPath()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" },
            new() { Id = UserId2, Name = "Bob" }
        };

        var (provider, storageMock) = BuildProvider(seed);
        var pathResolver = new BlobPathResolver(storageMock.Object);
        var blobName = pathResolver.GetBlobName(typeof(QueryTestUser));
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);

        var result = queryable
            .FirstOrDefault(u => u.Id == UserId2);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(UserId2);
        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.Never);
        storageMock.Verify(x => x.ReadWithMetadataAsync<QueryTestUser>($"{blobName}/{UserId2}.json"), Times.Once);
    }

    [Fact]
    public void FirstOrDefault_WithCapturedVariablePredicate_DoesNotThrow()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" },
            new() { Id = UserId2, Name = "Bob" }
        };

        var (provider, storageMock) = BuildProvider(seed);
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);
        const string userId = UserId2;

        var result = Should.NotThrow(() => queryable.FirstOrDefault(u => u.Id == userId));

        result.ShouldNotBeNull();
        result.Id.ShouldBe(UserId2);
        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.AtMostOnce);
    }

    [Fact]
    public void FirstOrDefault_WithPrimaryKeyGreaterThan_ReadsOnlyMatchingRangeBlobs()
    {
        var seed = new List<RangeQueryTestUser>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" },
            new() { Id = 3, Name = "C" }
        };

        var (provider, storageMock) = BuildRangeProvider(seed);
        var pathResolver = new BlobPathResolver(storageMock.Object);
        var blobName = pathResolver.GetBlobName(typeof(RangeQueryTestUser));
        var queryable = new CloudStorageQueryable<RangeQueryTestUser>(provider);

        var result = queryable.FirstOrDefault(u => u.Id > 1);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(2);
        storageMock.Verify(x => x.ListAsync(blobName), Times.Once);
        storageMock.Verify(x => x.ReadWithMetadataAsync<RangeQueryTestUser?>($"{blobName}/1.json"), Times.Never);
        storageMock.Verify(x => x.ReadWithMetadataAsync<RangeQueryTestUser?>($"{blobName}/2.json"), Times.Once);
        storageMock.Verify(x => x.ReadWithMetadataAsync<RangeQueryTestUser?>($"{blobName}/3.json"), Times.Once);
    }

    [Fact]
    public void Where_WithPrimaryKeyLessThanOrEqual_ReadsOnlyMatchingRangeBlobs()
    {
        var seed = new List<RangeQueryTestUser>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" },
            new() { Id = 3, Name = "C" }
        };

        var (provider, storageMock) = BuildRangeProvider(seed);
        var pathResolver = new BlobPathResolver(storageMock.Object);
        var blobName = pathResolver.GetBlobName(typeof(RangeQueryTestUser));
        var queryable = new CloudStorageQueryable<RangeQueryTestUser>(provider);

        var results = ((IQueryable<RangeQueryTestUser>)queryable).Where(u => u.Id <= 2).ToList();

        results.Count.ShouldBe(2);
        results.Select(x => x.Id).Order().ShouldBe([1, 2]);
        storageMock.Verify(x => x.ListAsync(blobName), Times.Once);
        storageMock.Verify(x => x.ReadWithMetadataAsync<RangeQueryTestUser?>($"{blobName}/1.json"), Times.Once);
        storageMock.Verify(x => x.ReadWithMetadataAsync<RangeQueryTestUser?>($"{blobName}/2.json"), Times.Once);
        storageMock.Verify(x => x.ReadWithMetadataAsync<RangeQueryTestUser?>($"{blobName}/3.json"), Times.Never);
    }

    [Fact]
    public void FirstOrDefault_WithPrimaryKeyBetweenBounds_ReadsOnlyMatchingBlob()
    {
        var seed = new List<RangeQueryTestUser>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" },
            new() { Id = 3, Name = "C" }
        };

        var (provider, storageMock) = BuildRangeProvider(seed);
        var pathResolver = new BlobPathResolver(storageMock.Object);
        var blobName = pathResolver.GetBlobName(typeof(RangeQueryTestUser));
        var queryable = new CloudStorageQueryable<RangeQueryTestUser>(provider);

        var result = queryable.FirstOrDefault(u => u.Id > 1 && u.Id <= 2);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(2);
        storageMock.Verify(x => x.ReadWithMetadataAsync<RangeQueryTestUser?>($"{blobName}/1.json"), Times.Never);
        storageMock.Verify(x => x.ReadWithMetadataAsync<RangeQueryTestUser?>($"{blobName}/2.json"), Times.Once);
        storageMock.Verify(x => x.ReadWithMetadataAsync<RangeQueryTestUser?>($"{blobName}/3.json"), Times.Never);
    }

    [Fact]
    public void FirstOrDefault_WithReversedPrimaryKeyComparison_IsSupported()
    {
        var seed = new List<RangeQueryTestUser>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" },
            new() { Id = 3, Name = "C" }
        };

        var (provider, storageMock) = BuildRangeProvider(seed);
        var pathResolver = new BlobPathResolver(storageMock.Object);
        var blobName = pathResolver.GetBlobName(typeof(RangeQueryTestUser));
        var queryable = new CloudStorageQueryable<RangeQueryTestUser>(provider);

        var result = queryable.FirstOrDefault(u => 2 < u.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(3);
        storageMock.Verify(x => x.ReadWithMetadataAsync<RangeQueryTestUser?>($"{blobName}/1.json"), Times.Never);
        storageMock.Verify(x => x.ReadWithMetadataAsync<RangeQueryTestUser?>($"{blobName}/2.json"), Times.Never);
        storageMock.Verify(x => x.ReadWithMetadataAsync<RangeQueryTestUser?>($"{blobName}/3.json"), Times.Once);
    }

    [Fact]
    public void Execute_NonGeneric_ReturnsResult()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" }
        };

        var (provider, _) = BuildProvider(seed);
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);
        var expression = ((IQueryable<QueryTestUser>)queryable).Where(u => u.Name == "Alice").Expression;

        var result = provider.Execute(expression);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void Take_UsesPagedListingAndReadsOnlyRequestedEntities()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" },
            new() { Id = UserId2, Name = "Bob" }
        };

        var (provider, storageMock) = BuildProvider(seed);
        var pathResolver = new BlobPathResolver(storageMock.Object);
        var blobName = pathResolver.GetBlobName(typeof(QueryTestUser));
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);

        var results = ((IQueryable<QueryTestUser>)queryable).Take(1).ToList();

        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(UserId1);
        storageMock.Verify(x => x.ListPageAsync(blobName, It.IsAny<int>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.Never);
        storageMock.Verify(x => x.ReadWithMetadataAsync<QueryTestUser?>(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void SkipTake_WithPrimaryKeyRange_UsesPagedRangeLoading()
    {
        var seed = new List<RangeQueryTestUser>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" },
            new() { Id = 3, Name = "C" }
        };

        var (provider, storageMock) = BuildRangeProvider(seed);
        var pathResolver = new BlobPathResolver(storageMock.Object);
        var blobName = pathResolver.GetBlobName(typeof(RangeQueryTestUser));
        var queryable = new CloudStorageQueryable<RangeQueryTestUser>(provider);

        var results = ((IQueryable<RangeQueryTestUser>)queryable)
            .Where(u => u.Id >= 1)
            .Skip(1)
            .Take(1)
            .ToList();

        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(2);
        storageMock.Verify(x => x.ListPageAsync(blobName, It.IsAny<int>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
        storageMock.Verify(
            x => x.ReadWithMetadataAsync<RangeQueryTestUser?>(It.Is<string>(p => p == $"{blobName}/1.json")),
            Times.Never);
        storageMock.Verify(
            x => x.ReadWithMetadataAsync<RangeQueryTestUser?>(It.Is<string>(p => p == $"{blobName}/2.json")),
            Times.Once);
    }

    [Fact]
    public void SkipTake_WithNonPrimaryKeyPredicate_FallsBackToFullLoad()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" },
            new() { Id = UserId2, Name = "Bob" }
        };

        var (provider, storageMock) = BuildProvider(seed);
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);

        var results = ((IQueryable<QueryTestUser>)queryable)
            .Where(u => u.Name.Contains("o"))
            .Skip(0)
            .Take(1)
            .ToList();

        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(UserId2);
        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void SkipTake_WithPrimaryKeyEquality_UsesDirectLookup()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" }
        };

        var (provider, storageMock) = BuildProvider(seed);
        var pathResolver = new BlobPathResolver(storageMock.Object);
        var blobName = pathResolver.GetBlobName(typeof(QueryTestUser));
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);

        var results = ((IQueryable<QueryTestUser>)queryable)
            .Where(u => u.Id == UserId1)
            .Skip(0)
            .Take(1)
            .ToList();

        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(UserId1);
        storageMock.Verify(x => x.ReadWithMetadataAsync<QueryTestUser>($"{blobName}/{UserId1}.json"), Times.Once);
        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.Never);
        storageMock.Verify(
            x => x.ListPageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void SkipWithoutTake_FallsBackToFullLoad()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" },
            new() { Id = UserId2, Name = "Bob" }
        };

        var (provider, storageMock) = BuildProvider(seed);
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);

        var results = ((IQueryable<QueryTestUser>)queryable).Skip(1).ToList();

        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(UserId2);
        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.Once);
        storageMock.Verify(
            x => x.ListPageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void TakeZero_ReturnsEmptyWithoutListing()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" }
        };

        var (provider, storageMock) = BuildProvider(seed);
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);

        var results = ((IQueryable<QueryTestUser>)queryable).Take(0).ToList();

        results.ShouldBeEmpty();
        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.Never);
        storageMock.Verify(
            x => x.ListPageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
        storageMock.Verify(x => x.ReadWithMetadataAsync<QueryTestUser?>(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void SkipTake_WithNegativeTake_FallsBackToFullLoad()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" },
            new() { Id = UserId2, Name = "Bob" }
        };

        var (provider, storageMock) = BuildProvider(seed);
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);

        var results = ((IQueryable<QueryTestUser>)queryable).Skip(0).Take(-1).ToList();

        results.ShouldNotBeNull();
        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.Once);
        storageMock.Verify(
            x => x.ListPageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("select")]
    [InlineData("selectmany")]
    [InlineData("reverse")]
    [InlineData("distinct")]
    public void SkipTake_WithUnsupportedOperators_FallsBackToFullLoad(string mode)
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" },
            new() { Id = UserId2, Name = "Bob" }
        };

        var (provider, storageMock) = BuildProvider(seed);
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);

        switch (mode)
        {
            case "select":
                _ = ((IQueryable<QueryTestUser>)queryable).Select(x => x).Skip(0).Take(1).ToList();
                break;
            case "selectmany":
                _ = ((IQueryable<QueryTestUser>)queryable)
                    .SelectMany(x => new[] { x })
                    .Skip(0)
                    .Take(1)
                    .ToList();
                break;
            case "reverse":
                _ = ((IQueryable<QueryTestUser>)queryable).Reverse().Skip(0).Take(1).ToList();
                break;
            case "distinct":
                _ = queryable.Distinct().Skip(0).Take(1).ToList();
                break;
        }

        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.Once);
        storageMock.Verify(
            x => x.ListPageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void SkipTakeCount_ScalarResult_DoesNotUsePushdown()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" },
            new() { Id = UserId2, Name = "Bob" }
        };

        var (provider, storageMock) = BuildProvider(seed);
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);

        var count = ((IQueryable<QueryTestUser>)queryable).Skip(1).Take(1).Count();

        count.ShouldBe(2);
        storageMock.Verify(x => x.ListAsync(It.IsAny<string>()), Times.Once);
        storageMock.Verify(
            x => x.ListPageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsyncEnumerator_ReturnsAllItems()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" },
            new() { Id = UserId2, Name = "Bob" }
        };

        var (provider, _) = BuildProvider(seed);
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);

        var results = new List<QueryTestUser>();
        await foreach (var item in queryable)
        {
            results.Add(item);
        }

        results.Count.ShouldBe(2);
        results.Select(x => x.Id).ShouldBe([UserId1, UserId2]);
    }

    [Fact]
    public async Task GetAsyncEnumerator_WithEmptySet_ReturnsNoItems()
    {
        var (provider, _) = BuildProvider([]);
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);

        var count = 0;
        await foreach (var _ in queryable)
        {
            count++;
        }

        count.ShouldBe(0);
    }

    [Fact]
    public void IEnumerable_GetEnumerator_DelegatesToGenericEnumerator()
    {
        var seed = new List<QueryTestUser>
        {
            new() { Id = UserId1, Name = "Alice" }
        };

        var (provider, _) = BuildProvider(seed);
        var queryable = new CloudStorageQueryable<QueryTestUser>(provider);

        IEnumerable nonGeneric = queryable;
        var iterator = nonGeneric.GetEnumerator();
        using var iterator1 = iterator as IDisposable;

        iterator.MoveNext().ShouldBeTrue();
        ((QueryTestUser?)iterator.Current)?.Id.ShouldBe(UserId1);
        iterator.MoveNext().ShouldBeFalse();
    }


    // ── helpers ───────────────────────────────────────────────────────────────

    private static void storageProviderMock_SetupMissingRead(
        Mock<IStorageProvider> mock, string path)
    {
        mock.Setup(x => x.ReadWithMetadataAsync<QueryTestUser>(It.Is<string>(p => p == path)))
            .ReturnsAsync(new StorageObject<QueryTestUser>(null, null, false));
        mock.Setup(x => x.ReadWithMetadataAsync<QueryTestUser?>(It.Is<string>(p => p == path)))
            .ReturnsAsync(new StorageObject<QueryTestUser?>(null, null, false));
    }

    private static void SetupPagedListing(Mock<IStorageProvider> storageProviderMock,
        IReadOnlyList<string> orderedPaths)
    {
        storageProviderMock
            .Setup(x => x.ListPageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, int pageSize, string? continuationToken, CancellationToken _) =>
            {
                var start = 0;
                if (!string.IsNullOrWhiteSpace(continuationToken))
                {
                    _ = int.TryParse(continuationToken, out start);
                }

                var keys = orderedPaths.Skip(start).Take(pageSize).ToList();
                var nextIndex = start + keys.Count;
                var hasMore = nextIndex < orderedPaths.Count;
                return new StorageListPage(keys, hasMore ? nextIndex.ToString() : null, hasMore);
            });
    }
}

// ── test model ────────────────────────────────────────────────────────────────

public class QueryTestUser
{
    [StringLength(256)] public string Id { get; init; } = string.Empty;

    [StringLength(256)] public string Name { get; init; } = string.Empty;
}

public class RangeQueryTestUser
{
    public int Id { get; init; }

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Name { get; init; } = string.Empty;
}

public class MinimalDbContext(DbContextOptions<MinimalDbContext> options) : DbContext(options)
{
    public DbSet<QueryTestUser> Users => Set<QueryTestUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var userEntity = modelBuilder.Entity<QueryTestUser>();
        userEntity.HasKey(u => u.Id);
        userEntity.Property(u => u.Id).HasMaxLength(256);
        userEntity.Property(u => u.Name).HasMaxLength(256);
    }
}

public class RangeMinimalDbContext(DbContextOptions<RangeMinimalDbContext> options) : DbContext(options)
{
    public DbSet<RangeQueryTestUser> Users => Set<RangeQueryTestUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var userEntity = modelBuilder.Entity<RangeQueryTestUser>();
        userEntity.HasKey(u => u.Id);
        userEntity.Property(u => u.Name).HasMaxLength(256);
    }
}