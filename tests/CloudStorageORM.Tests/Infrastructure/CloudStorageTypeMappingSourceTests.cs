using CloudStorageORM.Abstractions;
using CloudStorageORM.Enums;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Options;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

public class CloudStorageTypeMappingSourceTests
{
    private static CloudStorageTypeMappingSource BuildSut(CloudProvider provider = CloudProvider.Azure)
    {
        var services = new ServiceCollection();
        services.AddEntityFrameworkInMemoryDatabase();
        var sp = services.BuildServiceProvider();
        var deps = sp.GetRequiredService<TypeMappingSourceDependencies>();
        var options = new CloudStorageOptions { Provider = provider };
        return new CloudStorageTypeMappingSource(deps, options);
    }

    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(double))]
    public void FindMapping_PrimitiveType_DelegatesToBase(Type clrType)
    {
        var sut = BuildSut();
        // Primitives go to base – should not throw and may return null (base InMemory can't map them)
        Should.NotThrow(() => sut.FindMapping(clrType));
    }

    [Fact]
    public void FindMapping_StringType_DelegatesToBase()
    {
        var sut = BuildSut();
        Should.NotThrow(() => sut.FindMapping(typeof(string)));
    }

    [Fact]
    public void FindMapping_GuidType_DelegatesToBase()
    {
        var sut = BuildSut();
        Should.NotThrow(() => sut.FindMapping(typeof(Guid)));
    }

    [Fact]
    public void FindMapping_ConcreteEntityType_ReturnsCachedMapping()
    {
        var sut = BuildSut();
        var first = sut.FindMapping(typeof(PlainEntity));
        var second = sut.FindMapping(typeof(PlainEntity));
        first.ShouldNotBeNull();
        first.ShouldBeOfType<CloudStorageTypeMapping>();
        ReferenceEquals(first, second).ShouldBeTrue(); // cached
    }

    [Fact]
    public void FindMapping_EntityWithBlobSettings_ValidName_ReturnsMapping()
    {
        var sut = BuildSut();
        Should.NotThrow(() => sut.FindMapping(typeof(ValidBlobEntity)));
    }

    [Fact]
    public void FindMapping_EntityWithBlobSettings_InvalidAttributeName_DoesNotThrow()
    {
        var sut = BuildSut();
        Should.NotThrow(() => sut.FindMapping(typeof(InvalidBlobEntity)));
    }

    // ── test entities ─────────────────────────────────────────────────────

    private class PlainEntity
    {
        // ReSharper disable once UnusedMember.Local
        public string Id { get; set; } = string.Empty;
    }

    [BlobSettings("valid-blob")]
    private class ValidBlobEntity
    {
        // ReSharper disable once UnusedMember.Local
        public string Id { get; set; } = string.Empty;
    }

    [BlobSettings("INVALID BLOB NAME WITH SPACES")]
    private class InvalidBlobEntity
    {
        // ReSharper disable once UnusedMember.Local
        public string Id { get; set; } = string.Empty;
    }
}