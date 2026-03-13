using CloudStorageORM.Contexts;
using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CloudStorageORM.Tests.Contexts;

/// <summary>
/// Tests for CloudStorageDbContext constructor.
/// 
/// NOTE: The success path (valid options and storage provider initialization) requires Azurite
/// (Azure Storage emulator) to be running and is tested in integration tests. This class focuses
/// on unit testing error paths and boundary conditions.
/// </summary>
public class CloudStorageDbContextTests
{
    [Fact]
    public void Constructor_WithoutCloudStorageExtension_ThrowsInvalidCastException()
    {
        var options = new DbContextOptionsBuilder().Options;

        var ex = Should.Throw<InvalidCastException>(() => new CloudStorageDbContext(options));

        ex.Message.ShouldContain("CloudStorageOptions");
    }

    [Fact]
    public void Constructor_WithUnsupportedProvider_ThrowsNotSupportedException()
    {
        var builder = new DbContextOptionsBuilder();
        builder.UseCloudStorageOrm(options =>
        {
            options.Provider = CloudProvider.Aws;
            options.ConnectionString = "ignored";
            options.ContainerName = "ignored";
        });

        var ex = Should.Throw<NotSupportedException>(() => new CloudStorageDbContext(builder.Options));

        ex.Message.ShouldContain("not supported");
    }
}