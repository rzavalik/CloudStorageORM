namespace CloudStorageORM.Tests.Infrastructure
{
    using System.Collections.Generic;

    namespace CloudStorageORM.Tests.Infrastructure
    {
        using System;
        using global::CloudStorageORM.Abstractions;
        using global::CloudStorageORM.Infrastructure;
        using global::CloudStorageORM.Interfaces.Infrastructure;
        using global::CloudStorageORM.Interfaces.StorageProviders;
        using Microsoft.EntityFrameworkCore.ChangeTracking;
        using Microsoft.EntityFrameworkCore.Infrastructure;
        using Microsoft.EntityFrameworkCore.Metadata;
        using Microsoft.EntityFrameworkCore.Update;
        using Moq;
        using Shouldly;
        using Xunit;

        public class BlobPathResolverTests
        {
            private IBlobPathResolver MakeSut(IStorageProvider storageProvider)
            {
                return new BlobPathResolver(storageProvider);
            }

            [Fact]
            public void GetBlobNameWithBlobSettingsAttribute_UsesCustomName()
            {
                var mockProvider = new Mock<IStorageProvider>();
                mockProvider
                    .Setup(x => x.SanitizeBlobName(It.IsAny<string>()))
                    .Returns<string>(s => $"{s}");
                var sut = MakeSut(mockProvider.Object);

                var result = sut.GetBlobName(typeof(CustomNamedEntity));

                result.ShouldBe("baa75315fcdd71d5-customnamedentity");
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

                result.ShouldBe("ae8110d245d262ef-genericentity_1");
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
            public void GetPathWithoutPrimaryKeyValue_ThrowsException()
            {
                var mockEntry = new Mock<IUpdateEntry>();
                var mockType = new Mock<IEntityType>();
                var mockProperty = new Mock<IProperty>();

                mockType.Setup(x => x.ClrType).Returns(typeof(DummyEntity));
                mockType.Setup(x => x.FindPrimaryKey()).Returns(new TestKey(mockProperty.Object));
                mockEntry.Setup(x => x.EntityType).Returns(mockType.Object);
                mockEntry.Setup(x => x.GetCurrentValue(mockProperty.Object)).Returns(null);

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

            private class TestKey : IKey
            {
                public TestKey(params IProperty[] properties) => Properties = properties;

                public object? this[string name] => throw new NotImplementedException();

                public IReadOnlyList<IProperty> Properties { get; }

                public IEntityType DeclaringEntityType => throw new NotImplementedException();

                IReadOnlyList<IReadOnlyProperty> IReadOnlyKey.Properties => Properties;

                IReadOnlyEntityType IReadOnlyKey.DeclaringEntityType => DeclaringEntityType;

                public IAnnotation AddRuntimeAnnotation(string name, object? value)
                {
                    throw new NotImplementedException();
                }

                public IAnnotation? FindAnnotation(string name)
                {
                    throw new NotImplementedException();
                }

                public IAnnotation? FindRuntimeAnnotation(string name)
                {
                    throw new NotImplementedException();
                }

                public IEnumerable<IAnnotation> GetAnnotations()
                {
                    throw new NotImplementedException();
                }

                public TValue GetOrAddRuntimeAnnotationValue<TValue, TArg>(string name, Func<TArg?, TValue> valueFactory, TArg? factoryArgument)
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

                public IAnnotation? RemoveRuntimeAnnotation(string name)
                {
                    throw new NotImplementedException();
                }

                public IAnnotation SetRuntimeAnnotation(string name, object? value)
                {
                    throw new NotImplementedException();
                }
            }

            [BlobSettings(Name = "custom-name")]
            private class CustomNamedEntity { }

            private class DummyEntity { }

            private class SubType1 { }

            private class SubType2 { }

            private class GenericEntity<T> { }
        }
    }

}
