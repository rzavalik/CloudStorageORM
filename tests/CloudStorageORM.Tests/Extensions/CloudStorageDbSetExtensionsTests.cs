using System.ComponentModel.DataAnnotations;
using CloudStorageORM.Contexts;
using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace CloudStorageORM.Tests.Extensions;

public class CloudStorageDbSetExtensionsTests
{
    [Fact]
    public async Task ClearAsync_WithNullDbSet_ThrowsArgumentNullException()
    {
        var options = new DbContextOptionsBuilder<LocalContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new LocalContext(options);
        DbSet<LocalUser> dbSet = null!;

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => dbSet.ClearAsync(context));

        ex.ParamName.ShouldBe("dbSet");
    }

    [Fact]
    public async Task ClearAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var options = new DbContextOptionsBuilder<LocalContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new LocalContext(options);

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => context.Users.ClearAsync(null!));

        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task ClearAsync_WithStandardDbContext_RemovesAllEntities()
    {
        var options = new DbContextOptionsBuilder<LocalContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new LocalContext(options);
        context.Users.AddRange(
            new LocalUser { Id = "sample-user-001", Name = "A" },
            new LocalUser { Id = "sample-user-002", Name = "B" });
        await context.SaveChangesAsync();

        var cleared = await context.Users.ClearAsync(context);

        cleared.ShouldBe(2);
        (await context.Users.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task ClearAsync_WithStandardDbContext_WhenAlreadyEmpty_ReturnsZero()
    {
        var options = new DbContextOptionsBuilder<LocalContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new LocalContext(options);

        var cleared = await context.Users.ClearAsync(context);

        cleared.ShouldBe(0);
    }

    [Fact]
    public async Task ClearAsync_WithCloudStorageDbContext_DeletesKeysAndDetachesTrackedEntries()
    {
        var storageMock = new Mock<IStorageProvider>();
        var resolverMock = new Mock<IBlobPathResolver>();

        resolverMock.Setup(x => x.GetBlobName(typeof(CloudUser))).Returns("cloudusers");
        storageMock.Setup(x => x.ListAsync("cloudusers")).ReturnsAsync([
            "cloudusers/user-1.json",
            "cloudusers/user-2.json"
        ]);

        await using var context = CreateCloudContext(storageMock.Object, resolverMock.Object);
        var tracked = new CloudUser { Id = "user-1", Name = "A" };
        context.Users.Add(tracked);
        await context.SaveChangesAsync();

        var cleared = await context.Users.ClearAsync(context);

        cleared.ShouldBe(2);
        storageMock.Verify(x => x.ListAsync("cloudusers"), Times.Once);
        storageMock.Verify(x => x.DeleteAsync("cloudusers/user-1.json"), Times.Once);
        storageMock.Verify(x => x.DeleteAsync("cloudusers/user-2.json"), Times.Once);
        context.Entry(tracked).State.ShouldBe(EntityState.Detached);
    }

    [Fact]
    public async Task ClearAsync_WithCloudStorageDbContext_WhenNoKeys_ReturnsZeroAndSkipsDelete()
    {
        var storageMock = new Mock<IStorageProvider>();
        var resolverMock = new Mock<IBlobPathResolver>();

        resolverMock.Setup(x => x.GetBlobName(typeof(CloudUser))).Returns("cloudusers");
        storageMock.Setup(x => x.ListAsync("cloudusers")).ReturnsAsync([]);

        await using var context = CreateCloudContext(storageMock.Object, resolverMock.Object);

        var cleared = await context.Users.ClearAsync(context);

        cleared.ShouldBe(0);
        storageMock.Verify(x => x.ListAsync("cloudusers"), Times.Once);
        storageMock.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    private static TestCloudContext CreateCloudContext(
        IStorageProvider storageProvider,
        IBlobPathResolver pathResolver)
    {
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Aws,
            ContainerName = "test-bucket",
            Aws = new CloudStorageAwsOptions
            {
                AccessKeyId = "test",
                SecretAccessKey = "test",
                Region = "us-east-1",
                ServiceUrl = "http://localhost:4566",
                ForcePathStyle = true
            }
        };

        var services = new ServiceCollection();
        services.AddEntityFrameworkInMemoryDatabase();
        services.AddEntityFrameworkCloudStorageOrm(options);
        services.AddSingleton(storageProvider);
        services.AddSingleton(pathResolver);

        var internalProvider = services.BuildServiceProvider();

        var builder = new DbContextOptionsBuilder<TestCloudContext>();
        builder.UseInMemoryDatabase(Guid.NewGuid().ToString());
        builder.UseCloudStorageOrm(cfg =>
        {
            cfg.Provider = options.Provider;
            cfg.ContainerName = options.ContainerName;
            cfg.Aws = options.Aws;
        });
        builder.UseInternalServiceProvider(internalProvider);

        return new TestCloudContext(builder.Options);
    }

    private sealed class LocalContext(DbContextOptions<LocalContext> options) : DbContext(options)
    {
        public DbSet<LocalUser> Users => Set<LocalUser>();
    }

    private sealed class LocalUser
    {
        [MaxLength(100)]
        public string Id { get; init; } = string.Empty;

        [MaxLength(100)]
        public string Name { get; init; } = string.Empty;
    }

    private sealed class CloudUser
    {
        [Key]
        [MaxLength(100)]
        public string Id { get; init; } = string.Empty;

        [MaxLength(100)]
        public string Name { get; init; } = string.Empty;
    }

    private sealed class TestCloudContext(DbContextOptions<TestCloudContext> options) : CloudStorageDbContext(options)
    {
        public DbSet<CloudUser> Users => Set<CloudUser>();
    }
}