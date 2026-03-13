using CloudStorageORM.Enums;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Validators;
using Microsoft.EntityFrameworkCore.Metadata;
using Moq;
using Shouldly;

namespace CloudStorageORM.Tests.Validators;

public class CloudStorageModelValidatorTests
{
    [Fact]
    public void Ctor_WithNullStorageProvider_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new CloudStorageModelValidator(null!));
    }

    [Fact]
    public void Validate_WithValidModel_DoesNotThrow()
    {
        var storage = new Mock<IStorageProvider>();
        storage.SetupGet(x => x.CloudProvider).Returns(CloudProvider.Azure);
        storage.Setup(x => x.SanitizeBlobName(It.IsAny<string>())).Returns<string>(x => x);

        var model = new Mock<IMutableModel>();
        var entity = new Mock<IMutableEntityType>();
        entity.SetupGet(x => x.ClrType).Returns(typeof(ValidEntity));
        entity.SetupGet(x => x.Name).Returns(nameof(ValidEntity));
        entity.Setup(x => x.FindPrimaryKey()).Returns(Mock.Of<IMutableKey>());
        model.Setup(x => x.GetEntityTypes()).Returns(new List<IMutableEntityType> { entity.Object });

        var sut = new CloudStorageModelValidator(storage.Object);

        Should.NotThrow(() => sut.Validate(model.Object));
    }

    private sealed class ValidEntity
    {
        // ReSharper disable once UnusedMember.Local
        public string Id { get; set; } = string.Empty;
    }
}