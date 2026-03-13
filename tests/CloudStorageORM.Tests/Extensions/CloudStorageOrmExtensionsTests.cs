using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using CloudStorageORM.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CloudStorageORM.Tests.Extensions;

public class CloudStorageOrmExtensionsTests
{
    [Fact]
    public void UseCloudStorageOrm_NonGeneric_AddsExtension()
    {
        var builder = new DbContextOptionsBuilder();

        builder.UseCloudStorageOrm(options =>
        {
            options.Provider = CloudProvider.Azure;
            options.ConnectionString = "UseDevelopmentStorage=true";
            options.ContainerName = "unit-tests";
        });

        var extension = builder.Options.FindExtension<CloudStorageOrmOptionsExtension>();
        extension.ShouldNotBeNull();
        extension.Options.Provider.ShouldBe(CloudProvider.Azure);
        extension.Options.ContainerName.ShouldBe("unit-tests");
    }

    [Fact]
    public void UseCloudStorageOrm_Generic_AddsExtension()
    {
        var builder = new DbContextOptionsBuilder<LocalContext>();

        builder.UseCloudStorageOrm(options =>
        {
            options.Provider = CloudProvider.Azure;
            options.ConnectionString = "UseDevelopmentStorage=true";
            options.ContainerName = "generic-tests";
        });

        var extension = builder.Options.FindExtension<CloudStorageOrmOptionsExtension>();
        extension.ShouldNotBeNull();
        extension.Options.ContainerName.ShouldBe("generic-tests");
    }

    [Fact]
    public void UseCloudStorageOrm_WithNullConfigure_DoesNotThrow()
    {
        var builder = new DbContextOptionsBuilder();
        Should.NotThrow(() => builder.UseCloudStorageOrm(null!));
    }

    [Fact]
    public void UseCloudStorageOrm_CalledTwice_ReusesExistingExtension()
    {
        var builder = new DbContextOptionsBuilder();

        builder.UseCloudStorageOrm(options =>
        {
            options.Provider = CloudProvider.Azure;
            options.ConnectionString = "UseDevelopmentStorage=true";
            options.ContainerName = "first-call";
        });

        builder.UseCloudStorageOrm(options =>
        {
            options.Provider = CloudProvider.Azure;
            options.ConnectionString = "UseDevelopmentStorage=true";
            options.ContainerName = "second-call";
        });

        var extension = builder.Options.FindExtension<CloudStorageOrmOptionsExtension>();
        extension.ShouldNotBeNull();
        // The extension should have been found and reused from the first call
        extension.Options.ContainerName.ShouldBe("first-call");
    }

    private sealed class LocalContext(DbContextOptions<LocalContext> options) : DbContext(options);
}