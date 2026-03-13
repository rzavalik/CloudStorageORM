using CloudStorageORM.Abstractions;
using CloudStorageORM.Extensions;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CloudStorageORM.Tests.Extensions;

public class ModelBuilderExtensionsTests
{
    private const string AnnotationsConstantBlobName = "CloudStorageORM:BlobName";

    [BlobSettings("__ModelA")]
    private class ModelA
    {
    }

    [BlobSettings("")]
    private class ModelWithEmptyBlobName
    {
    }

    private class ModelB
    {
    }

    private class ModelC : ModelA
    {
    }

    private static ModelBuilder MakeModelBuilderWith(params Type[] types)
    {
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

    [Fact]
    public void ApplyBlobSettingsConventions_WithExplicitBlobSettingName_UsesAttributeValue()
    {
        // BlobSettings with explicit Name should use that name
        var modelBuilder = MakeModelBuilderWith(typeof(ModelA));

        modelBuilder.ApplyBlobSettingsConventions();
        var entityType = modelBuilder.Model.FindEntityType(typeof(ModelA));

        // ModelA has BlobSettings(Name = "__ModelA")
        entityType!.GetAnnotation(AnnotationsConstantBlobName).Value.ShouldBe("__modela");
    }

    [Fact]
    public void ApplyBlobSettingsConventions_WithEmptyBlobSettingName_ThrowsInvalidOperationException()
    {
        // Empty names are rejected after trimming because the resulting blob name is invalid.
        var modelBuilder = MakeModelBuilderWith(typeof(ModelWithEmptyBlobName));

        var ex = Should.Throw<InvalidOperationException>(() => modelBuilder.ApplyBlobSettingsConventions());

        ex.Message.ShouldContain("has no Blob name");
    }

    [Fact]
    public void ApplyBlobSettingsConventions_WithEntityWithNullBlobSettingName_UsesClassNameAsDefault()
    {
        // BlobSettings with null Name should use class name
        var modelBuilder = MakeModelBuilderWith(typeof(ModelB));

        modelBuilder.ApplyBlobSettingsConventions();
        var entityType = modelBuilder.Model.FindEntityType(typeof(ModelB));

        // ModelB doesn't have BlobSettings, so it should use the class name
        entityType!.GetAnnotation(AnnotationsConstantBlobName).Value.ShouldBe("modelb");
    }

    [Fact]
    public void ApplyBlobSettingsConventions_ReturnsModelBuilderForChaining()
    {
        var modelBuilder = MakeModelBuilderWith(typeof(ModelA));

        var result = modelBuilder.ApplyBlobSettingsConventions();

        result.ShouldBeSameAs(modelBuilder);
    }
}