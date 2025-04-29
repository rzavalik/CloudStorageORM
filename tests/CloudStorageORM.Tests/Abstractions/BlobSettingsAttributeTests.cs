namespace CloudStorageORM.Tests.Abstractions
{
    using System;
    using System.Linq;
    using CloudStorageORM.Abstractions;
    using Shouldly;

    public class BlobSettingsAttributeTests
    {
        [Fact]
        public void Constructor_WithValidPrefix_ShouldSetPrefix()
        {
            var name = "customer";

            var attribute = new BlobSettingsAttribute(name);

            attribute.Name.ShouldBe(name);
        }

        [Fact]
        public void Constructor_WithSpaces_ShouldSetPrefixTrimmed()
        {
            var name = " customer ";
            var expectedName = "customer";

            var attribute = new BlobSettingsAttribute(name);

            attribute.Name.ShouldBe(expectedName);
        }

        [Fact]
        public void Constructor_WithNullPrefix_ShouldThrowArgumentNullException()
        {
            var exception = Should.Throw<ArgumentNullException>(
                static () => new BlobSettingsAttribute(blobName: null));

            exception.ParamName.ShouldBe("blobName");
        }

        [Fact]
        public void AttributeUsage_ShouldTargetClassOnly_AndNotAllowMultiple()
        {
            var attributeUsage = typeof(BlobSettingsAttribute)
                .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .Cast<AttributeUsageAttribute>()
                .Single();

            attributeUsage.ValidOn.ShouldBe(AttributeTargets.Class);
            attributeUsage.AllowMultiple.ShouldBeFalse();
        }
    }
}
