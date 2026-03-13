using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Options;
using Shouldly;

namespace CloudStorageORM.Tests.Extensions;

public class CloudStorageOrmOptionsExtensionInfoTests
{
    [Fact]
    public void Properties_ReturnExpectedValues()
    {
        var options = new CloudStorageOptions
        {
            Provider = CloudProvider.Azure,
            ConnectionString = "UseDevelopmentStorage=true",
            ContainerName = "test"
        };

        var extension = new CloudStorageOrmOptionsExtension(options);
        var info = new CloudStorageOrmOptionsExtensionInfo(extension);

        info.IsDatabaseProvider.ShouldBeTrue();
        info.LogFragment.ShouldContain("CloudStorageORM");
        info.GetServiceProviderHashCode().ShouldBe(options.ConnectionString.GetHashCode());
    }

    [Fact]
    public void PopulateDebugInfo_DoesNotThrow()
    {
        var extension = new CloudStorageOrmOptionsExtension(new CloudStorageOptions());
        var info = new CloudStorageOrmOptionsExtensionInfo(extension);
        var debug = new Dictionary<string, string>();

        Should.NotThrow(() => info.PopulateDebugInfo(debug));
        debug.Count.ShouldBe(0);
    }

    [Fact]
    public void ShouldUseSameServiceProvider_AlwaysFalse()
    {
        var extension = new CloudStorageOrmOptionsExtension(new CloudStorageOptions());
        var info = new CloudStorageOrmOptionsExtensionInfo(extension);

        var other = new CloudStorageOrmOptionsExtensionInfo(
            new CloudStorageOrmOptionsExtension(new CloudStorageOptions()));
        info.ShouldUseSameServiceProvider(other).ShouldBeFalse();
    }

    [Fact]
    public void ExtensionProperty_ReturnsUnderlyingExtension()
    {
        var extension = new CloudStorageOrmOptionsExtension(new CloudStorageOptions());
        var info = new CloudStorageOrmOptionsExtensionInfo(extension);

        info.Extension.ShouldBeSameAs(extension);
    }
}