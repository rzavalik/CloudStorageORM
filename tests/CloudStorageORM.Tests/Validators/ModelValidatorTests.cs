using CloudStorageORM.Abstractions;
using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.Validators;
using CloudStorageORM.Validators;
using Microsoft.EntityFrameworkCore.Metadata;
using Moq;
using Shouldly;

namespace CloudStorageORM.Tests.Validators;

public class ModelValidatorTests
{
    [Fact]
    public void Ctor_WithNullBlobValidator_Throws()
    {
        var resolver = new Mock<IBlobPathResolver>().Object;
        Should.Throw<ArgumentNullException>(() => new ModelValidator(null!, resolver));
    }

    [Fact]
    public void Ctor_WithNullResolver_Throws()
    {
        var validator = new Mock<IBlobValidator>().Object;
        Should.Throw<ArgumentNullException>(() => new ModelValidator(validator, null!));
    }

    [Fact]
    public void Validate_WithValidEntity_DoesNotThrow()
    {
        var blobValidator = new Mock<IBlobValidator>();
        blobValidator.Setup(x => x.IsBlobNameValid(It.IsAny<string?>())).Returns(true);

        var resolver = new Mock<IBlobPathResolver>();
        resolver.Setup(x => x.GetBlobName(typeof(ValidEntity))).Returns("validentity");

        var sut = new ModelValidator(blobValidator.Object, resolver.Object);
        var model = BuildModel(typeof(ValidEntity), hasPrimaryKey: true);

        Should.NotThrow(() => sut.Validate(model));
    }

    [Fact]
    public void Validate_WithoutPrimaryKey_Throws()
    {
        var blobValidator = new Mock<IBlobValidator>();
        blobValidator.Setup(x => x.IsBlobNameValid(It.IsAny<string?>())).Returns(true);

        var resolver = new Mock<IBlobPathResolver>();
        resolver.Setup(x => x.GetBlobName(typeof(ValidEntity))).Returns("validentity");

        var sut = new ModelValidator(blobValidator.Object, resolver.Object);
        var model = BuildModel(typeof(ValidEntity), hasPrimaryKey: false);

        var ex = Should.Throw<InvalidOperationException>(() => sut.Validate(model));
        ex.Message.ShouldContain("primary key");
    }

    [Fact]
    public void Validate_WithInvalidBlobName_Throws()
    {
        var blobValidator = new Mock<IBlobValidator>();
        blobValidator.Setup(x => x.IsBlobNameValid(It.IsAny<string?>())).Returns(false);

        var resolver = new Mock<IBlobPathResolver>();
        resolver.Setup(x => x.GetBlobName(typeof(ValidEntity))).Returns("invalid");

        var sut = new ModelValidator(blobValidator.Object, resolver.Object);
        var model = BuildModel(typeof(ValidEntity), hasPrimaryKey: true);

        Should.Throw<InvalidOperationException>(() => sut.Validate(model));
    }

    [Fact]
    public void Validate_WithBlobSettingsAttribute_ValidatesAttributeName()
    {
        var blobValidator = new Mock<IBlobValidator>();
        blobValidator.Setup(x => x.IsBlobNameValid(It.IsAny<string?>())).Returns(true);

        var resolver = new Mock<IBlobPathResolver>();
        resolver.Setup(x => x.GetBlobName(typeof(AttributedEntity))).Returns("attributedentity");

        var sut = new ModelValidator(blobValidator.Object, resolver.Object);
        var model = BuildModel(typeof(AttributedEntity), hasPrimaryKey: true);

        Should.NotThrow(() => sut.Validate(model));
        blobValidator.Verify(x => x.IsBlobNameValid("custom-blob"), Times.AtLeastOnce);
    }

    [Fact]
    public void Validate_WithEmptyBlobSettingsName_SkipsResolverDefaultBlobNameValidation()
    {
        var blobValidator = new Mock<IBlobValidator>();
        blobValidator.Setup(x => x.IsBlobNameValid(It.IsAny<string?>())).Returns(true);

        var resolver = new Mock<IBlobPathResolver>(MockBehavior.Strict);

        var sut = new ModelValidator(blobValidator.Object, resolver.Object);
        var model = BuildModel(typeof(EmptyNameAttributedEntity), hasPrimaryKey: true);

        Should.NotThrow(() => sut.Validate(model));
        blobValidator.Verify(x => x.IsBlobNameValid(""), Times.Once);
        resolver.Verify(x => x.GetBlobName(It.IsAny<Type>()), Times.Never);
    }

    [Fact]
    public void Validate_WhenEntityCannotBeInstantiated_ThrowsWrappedSerializationException()
    {
        var blobValidator = new Mock<IBlobValidator>();
        blobValidator.Setup(x => x.IsBlobNameValid(It.IsAny<string?>())).Returns(true);

        var resolver = new Mock<IBlobPathResolver>();
        resolver.Setup(x => x.GetBlobName(typeof(NonConstructibleEntity))).Returns("nonconstructibleentity");

        var sut = new ModelValidator(blobValidator.Object, resolver.Object);
        var model = BuildModel(typeof(NonConstructibleEntity), hasPrimaryKey: true);

        var ex = Should.Throw<InvalidOperationException>(() => sut.Validate(model));

        ex.Message.ShouldContain("not serializable with System.Text.Json");
        ex.Message.ShouldContain(typeof(NonConstructibleEntity).FullName!);
        ex.InnerException.ShouldNotBeNull();
    }

    private static IMutableModel BuildModel(Type clrType, bool hasPrimaryKey)
    {
        var model = new Mock<IMutableModel>();
        var entity = new Mock<IMutableEntityType>();

        entity.SetupGet(x => x.ClrType).Returns(clrType);
        entity.SetupGet(x => x.Name).Returns(clrType.Name);
        entity.Setup(x => x.FindPrimaryKey()).Returns(hasPrimaryKey ? Mock.Of<IMutableKey>() : null);

        model.Setup(x => x.GetEntityTypes())
            .Returns(new List<IMutableEntityType> { entity.Object });

        return model.Object;
    }

    private sealed class ValidEntity
    {
        // ReSharper disable once UnusedMember.Local
        public string Id { get; set; } = string.Empty;
    }

    [BlobSettings("custom-blob")]
    private sealed class AttributedEntity
    {
        // ReSharper disable once UnusedMember.Local
        public string Id { get; set; } = string.Empty;
    }

    [BlobSettings("")]
    private sealed class EmptyNameAttributedEntity
    {
        // ReSharper disable once UnusedMember.Local
        public string Id { get; set; } = string.Empty;
    }

    private sealed class NonConstructibleEntity(string id)
    {
        // ReSharper disable once UnusedMember.Local
        public string Id { get; } = id;
    }
}