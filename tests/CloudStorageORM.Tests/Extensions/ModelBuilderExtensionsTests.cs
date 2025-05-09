namespace CloudStorageORM.Tests.Extensions
{
    using System;
    using CloudStorageORM.Abstractions;
    using CloudStorageORM.Extensions;
    using Microsoft.EntityFrameworkCore;
    using Shouldly;

    public class ModelBuilderExtensionsTests
    {
        private const string AnnotationsConstantBlobName = "CloudStorageORM:BlobName";

        [BlobSettings(Name = "__ModelA")]
        private class ModelA { }
        private class ModelB { }
        private class ModelC : ModelA { }

        private static ModelBuilder MakeModelBuilderWith(params Type[] types)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DbContext>();
            var modelBuilder = new ModelBuilder();
            foreach (var type in types)
            {
                modelBuilder.Entity(type);
            }
            return modelBuilder;
        }

        [Theory]
        [InlineData(typeof(ModelA))]
        [InlineData(typeof(ModelB))]
        [InlineData(typeof(ModelC))]
        public void ApplyBlobSettingsConventions_WithNameDefined_ShouldApplyAsDefined(Type type)
        {
            var modelBuilder = MakeModelBuilderWith(type);

            modelBuilder.ApplyBlobSettingsConventions();
            var entityType = modelBuilder
                .Model
                .GetEntityTypes()
                .First(e => e.ClrType == type);

            var expectedBlobName = entityType.ClrType.Name.ToLower();

            if (type == typeof(ModelA))
            {
                expectedBlobName = "__modela";
            }

            entityType
                .GetAnnotation(AnnotationsConstantBlobName)
                    .Value
                    .ShouldBe(expectedBlobName);
        }

        [Fact]
        public void ApplyBlobSettingsConventions_ShouldHandleMultipleEntities()
        {
            var modelBuilder = MakeModelBuilderWith(
                typeof(ModelA),
                typeof(ModelB),
                typeof(ModelC));

            modelBuilder.ApplyBlobSettingsConventions();

            modelBuilder.Model.GetEntityTypes().Count().ShouldBe(3);

            modelBuilder.Model.FindEntityType(typeof(ModelA))!
                .GetAnnotation(AnnotationsConstantBlobName)
                .Value
                .ShouldBe("__modela");

            modelBuilder.Model.FindEntityType(typeof(ModelB))!
                .GetAnnotation(AnnotationsConstantBlobName)
                .Value
                .ShouldBe("modelb");

            modelBuilder.Model.FindEntityType(typeof(ModelC))!
                .GetAnnotation(AnnotationsConstantBlobName)
                .Value
                .ShouldBe("modelc");
        }

        [Fact]
        public void ApplyBlobSettingsConventions_DuplicateEntities_ShouldNotThrow()
        {
            var modelBuilder = MakeModelBuilderWith(
                typeof(ModelA),
                typeof(ModelA),
                typeof(ModelB));

            var ex = Record.Exception(() => modelBuilder.ApplyBlobSettingsConventions());
            ex.ShouldBeNull();

            modelBuilder.Model
                .GetEntityTypes()
                .Count(e => e.ClrType == typeof(ModelA))
                .ShouldBe(1);
        }
    }
}
