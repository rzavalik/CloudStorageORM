using System.Reflection;
using System.Reflection.Emit;
using CloudStorageORM.Abstractions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Update;
using Moq;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

public class BlobPathResolverTests
{
    [Fact]
    public void Constructor_WithNullStorageProvider_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new BlobPathResolver(null!));

        ex.ParamName.ShouldBe("storageProvider");
    }

    private static IBlobPathResolver MakeSut(IStorageProvider storageProvider) => new BlobPathResolver(storageProvider);

    [Fact]
    public void GetBlobNameWithBlobSettingsAttribute_UsesCustomName()
    {
        var mockProvider = new Mock<IStorageProvider>();
        mockProvider
            .Setup(x => x.SanitizeBlobName(It.IsAny<string>()))
            .Returns<string>(s => $"{s}");
        var sut = MakeSut(mockProvider.Object);

        var result = sut.GetBlobName(typeof(CustomNamedEntity));

        result.ShouldEndWith("-customnamedentity");
        var hashPrefix = result.Split('-')[0];
        hashPrefix.Length.ShouldBe(16);
        hashPrefix.All(Uri.IsHexDigit).ShouldBeTrue();
    }

    [Fact]
    public void GetBlobNameWithoutAttribute_UsesSanitizedTypeName()
    {
        var mockProvider = new Mock<IStorageProvider>();
        mockProvider
            .Setup(x => x.SanitizeBlobName(It.IsAny<string>()))
            .Returns<string>(s => $"{s}");

        var sut = MakeSut(mockProvider.Object);

        var result = sut.GetBlobName(typeof(GenericEntity<SubType1>));

        result.ShouldEndWith("-genericentity_1");
        var hashPrefix = result.Split('-')[0];
        hashPrefix.Length.ShouldBe(16);
        hashPrefix.All(Uri.IsHexDigit).ShouldBeTrue();
    }

    [Fact]
    public void GetBlobNameForTwoSubEntities_ShouldNotCollide()
    {
        var mockProvider = new Mock<IStorageProvider>();
        mockProvider
            .Setup(x => x.SanitizeBlobName(It.IsAny<string>()))
            .Returns<string>(s => $"{s}");

        var sut = MakeSut(mockProvider.Object);

        var result1 = sut.GetBlobName(typeof(GenericEntity<SubType1>));
        var result2 = sut.GetBlobName(typeof(GenericEntity<SubType2>));

        result1.Equals(result2).ShouldBe(false);
    }

    [Fact]
    public void GetBlobName_WhenTypeFullNameIsNull_UsesTypeNameFallback()
    {
        var mockProvider = new Mock<IStorageProvider>();
        mockProvider
            .Setup(x => x.SanitizeBlobName(It.IsAny<string>()))
            .Returns<string>(s => s);

        var sut = MakeSut(mockProvider.Object);
        var genericParameterType = typeof(GenericEntity<>).GetGenericArguments()[0];

        var result = sut.GetBlobName(genericParameterType);

        result.ShouldEndWith("-t");
    }

    [Fact]
    public void GetBlobName_WithOpenGenericTypeBuilder_ResolvesWithoutFullNameFallbackErrors()
    {
        var mockProvider = new Mock<IStorageProvider>();
        mockProvider
            .Setup(x => x.SanitizeBlobName(It.IsAny<string>()))
            .Returns<string>(s => s);

        var sut = MakeSut(mockProvider.Object);

        var assemblyName = new AssemblyName($"BlobPathResolverDynamic_{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("Main");
        var typeBuilder = moduleBuilder.DefineType("DynamicGenericType`1", TypeAttributes.Public);
        typeBuilder.DefineGenericParameters("T");
        var dynamicGenericType = typeBuilder.AsType();

        var result = sut.GetBlobName(dynamicGenericType);

        result.ShouldEndWith("-dynamicgenerictype_1");
    }

    [Fact]
    public void GetPathWithoutPrimaryKeyValue_ThrowsException()
    {
        var mockEntry = new Mock<IUpdateEntry>();
        var mockType = new Mock<IEntityType>();
        var mockProperty = new Mock<IProperty>();

        mockType.Setup(x => x.ClrType).Returns(typeof(DummyEntity));
        mockType.Setup(x => x.FindPrimaryKey()).Returns(new TestKey(mockProperty.Object));
        mockEntry.Setup(x => x.EntityType).Returns(mockType.Object);
        mockEntry.Setup(x => x.GetCurrentValue(mockProperty.Object)).Returns(null!);

        var mockProvider = new Mock<IStorageProvider>();
        mockProvider.Setup(x => x.SanitizeBlobName(It.IsAny<string>())).Returns("entity");

        var sut = MakeSut(mockProvider.Object);

        Should.Throw<InvalidOperationException>(() => sut.GetPath(mockEntry.Object))
            .Message.ShouldContain("without a valid key value");
    }

    [Fact]
    public void GetPathWithValidKey_ReturnsExpectedPath()
    {
        var mockEntry = new Mock<IUpdateEntry>();
        var mockType = new Mock<IEntityType>();
        var mockProperty = new Mock<IProperty>();

        mockType.Setup(x => x.ClrType).Returns(typeof(DummyEntity));
        mockType.Setup(x => x.FindPrimaryKey()).Returns(new TestKey(mockProperty.Object));
        mockEntry.Setup(x => x.EntityType).Returns(mockType.Object);
        mockEntry.Setup(x => x.GetCurrentValue(mockProperty.Object)).Returns("123");

        var mockProvider = new Mock<IStorageProvider>();
        mockProvider.Setup(x => x.SanitizeBlobName(It.IsAny<string>())).Returns("dummy_entity");

        var sut = MakeSut(mockProvider.Object);

        var path = sut.GetPath(mockEntry.Object);

        path.ShouldBe("dummy_entity-dummy_entity/123.json");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetPath_WithInvalidKey_ThrowsInvalidOperationException(string? key)
    {
        var mockProvider = new Mock<IStorageProvider>();
        mockProvider.Setup(x => x.SanitizeBlobName(It.IsAny<string>())).Returns<string>(x => x);
        var sut = MakeSut(mockProvider.Object);

        var ex = Should.Throw<InvalidOperationException>(() => sut.GetPath(typeof(DummyEntity), key!));

        ex.Message.ShouldContain("without a valid key value");
    }

    [Fact]
    public void GetPath_WithNonStringKey_UsesToStringRepresentation()
    {
        var mockProvider = new Mock<IStorageProvider>();
        mockProvider.Setup(x => x.SanitizeBlobName(It.IsAny<string>())).Returns("dummy_entity");
        var sut = MakeSut(mockProvider.Object);

        var path = sut.GetPath(typeof(DummyEntity), 42);

        path.ShouldBe("dummy_entity-dummy_entity/42.json");
    }

    [Fact]
    public void GetPath_WhenPrimaryKeyMissing_ThrowsInvalidOperationException()
    {
        var mockEntry = new Mock<IUpdateEntry>();
        var mockType = new Mock<IEntityType>();
        mockType.Setup(x => x.Name).Returns("DummyEntity");
        mockType.Setup(x => x.ClrType).Returns(typeof(DummyEntity));
        mockType.Setup(x => x.FindPrimaryKey()).Returns((IKey?)null);
        mockEntry.Setup(x => x.EntityType).Returns(mockType.Object);
        mockEntry.Setup(x => x.GetCurrentValue(null!)).Returns((object?)null);

        var mockProvider = new Mock<IStorageProvider>();
        mockProvider.Setup(x => x.SanitizeBlobName(It.IsAny<string>())).Returns<string>(x => x);
        var sut = MakeSut(mockProvider.Object);

        var ex = Should.Throw<InvalidOperationException>(() => sut.GetPath(mockEntry.Object));

        ex.Message.ShouldContain("without a valid key value");
    }

    [Fact]
    public void GetPath_WhenPrimaryKeyHasNoProperties_ThrowsInvalidOperationException()
    {
        var mockEntry = new Mock<IUpdateEntry>();
        var mockType = new Mock<IEntityType>();
        mockType.Setup(x => x.Name).Returns("DummyEntity");
        mockType.Setup(x => x.ClrType).Returns(typeof(DummyEntity));
        mockType.Setup(x => x.FindPrimaryKey()).Returns(new TestKey());
        mockEntry.Setup(x => x.EntityType).Returns(mockType.Object);
        mockEntry.Setup(x => x.GetCurrentValue(null!)).Returns((object?)null);

        var mockProvider = new Mock<IStorageProvider>();
        mockProvider.Setup(x => x.SanitizeBlobName(It.IsAny<string>())).Returns<string>(x => x);
        var sut = MakeSut(mockProvider.Object);

        var ex = Should.Throw<InvalidOperationException>(() => sut.GetPath(mockEntry.Object));

        ex.Message.ShouldContain("without a valid key value");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Sanitize_WhenNameIsNullOrEmpty_ThrowsArgumentNullException(string? input)
    {
        var mockProvider = new Mock<IStorageProvider>();
        mockProvider.Setup(x => x.SanitizeBlobName(It.IsAny<string>())).Returns<string>(x => x);
        var resolver = new BlobPathResolver(mockProvider.Object);
        var sanitizeMethod = typeof(BlobPathResolver)
            .GetMethod("Sanitize", BindingFlags.Instance | BindingFlags.NonPublic);

        sanitizeMethod.ShouldNotBeNull();

        var ex = Should.Throw<TargetInvocationException>(() => sanitizeMethod.Invoke(resolver, [input]));
        ex.InnerException.ShouldBeOfType<ArgumentNullException>();
        ((ArgumentNullException)ex.InnerException!).ParamName.ShouldBe("name");
    }

    private class TestKey(params IProperty[] properties) : IKey
    {
        public object this[string name] => throw new NotImplementedException();

        public IReadOnlyList<IProperty> Properties { get; } = properties;

        public IEntityType DeclaringEntityType => throw new NotImplementedException();

        IReadOnlyList<IReadOnlyProperty> IReadOnlyKey.Properties => Properties;

        IReadOnlyEntityType IReadOnlyKey.DeclaringEntityType => DeclaringEntityType;

        public IAnnotation AddRuntimeAnnotation(string name, object? value)
        {
            throw new NotImplementedException();
        }

        public IAnnotation FindAnnotation(string name)
        {
            throw new NotImplementedException();
        }

        public IAnnotation FindRuntimeAnnotation(string name)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IAnnotation> GetAnnotations()
        {
            throw new NotImplementedException();
        }

        public TValue GetOrAddRuntimeAnnotationValue<TValue, TArg>(string name,
            Func<TArg?, TValue> valueFactory, TArg? factoryArgument)
        {
            throw new NotImplementedException();
        }

        public IPrincipalKeyValueFactory<TKey> GetPrincipalKeyValueFactory<TKey>() where TKey : notnull
        {
            throw new NotImplementedException();
        }

        public IPrincipalKeyValueFactory GetPrincipalKeyValueFactory()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IReadOnlyForeignKey> GetReferencingForeignKeys()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IAnnotation> GetRuntimeAnnotations()
        {
            throw new NotImplementedException();
        }

        public IAnnotation RemoveRuntimeAnnotation(string name)
        {
            throw new NotImplementedException();
        }

        public IAnnotation SetRuntimeAnnotation(string name, object? value)
        {
            throw new NotImplementedException();
        }
    }

    [BlobSettings("custom-name")]
    private class CustomNamedEntity
    {
    }

    private class DummyEntity
    {
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class SubType1
    {
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class SubType2
    {
    }

    // ReSharper disable once UnusedTypeParameter
    private class GenericEntity<T>
    {
    }
}