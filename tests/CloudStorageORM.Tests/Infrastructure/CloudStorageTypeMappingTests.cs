namespace CloudStorageORM.Tests.Infrastructure
{
    using global::CloudStorageORM.Infrastructure;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
    using Shouldly;

    public class CloudStorageTypeMappingTests
    {
        [Fact]
        public void Constructor_CreatesTypeMappingForClrType()
        {
            var mapping = new CloudStorageTypeMapping(typeof(string));
            mapping.ClrType.ShouldBe(typeof(string));
        }

        [Fact]
        public void WithComposedConverter_ReturnsNewInstance()
        {
            var mapping = new CloudStorageTypeMapping(typeof(string));
            var result = mapping.WithComposedConverter(null);
            result.ShouldNotBeNull();
            result.ShouldBeOfType<CloudStorageTypeMapping>();
        }

        [Fact]
        public void WithComposedConverter_WithConverter_ReturnsNewInstance()
        {
            var mapping = new CloudStorageTypeMapping(typeof(string));
            var converter = new ValueConverter<string, string>(v => v, v => v);
            var result = mapping.WithComposedConverter(converter);
            result.ShouldNotBeNull();
            result.ShouldBeOfType<CloudStorageTypeMapping>();
        }

        [Fact]
        public void WithComposedConverter_ReturnsDifferentInstance()
        {
            var mapping = new CloudStorageTypeMapping(typeof(string));
            var result = mapping.WithComposedConverter(null);
            ReferenceEquals(mapping, result).ShouldBeFalse();
        }

        [Fact]
        public void Clone_ReturnsNewCloudStorageTypeMapping()
        {
            var mapping = new TestCloudStorageTypeMapping(typeof(string));

            var clone = mapping.CloneForTest();

            clone.ShouldBeOfType<CloudStorageTypeMapping>();
            clone.ClrType.ShouldBe(typeof(string));
            ReferenceEquals(mapping, clone).ShouldBeFalse();
        }

        private sealed class TestCloudStorageTypeMapping(Type clrType) : CloudStorageTypeMapping(clrType)
        {
            public CoreTypeMapping CloneForTest()
            {
                return Clone(Parameters);
            }
        }
    }
}