using System.Linq.Expressions;
using CloudStorageORM.Abstractions;
using CloudStorageORM.Constants;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudStorageORM.Extensions;

public static class ModelBuilderExtensions
{
    extension(EntityTypeBuilder entityTypeBuilder)
    {
        public EntityTypeBuilder UseObjectETagConcurrency()
        {
            ArgumentNullException.ThrowIfNull(entityTypeBuilder);

            entityTypeBuilder.Property<string?>("ETag").IsConcurrencyToken();
            entityTypeBuilder.Metadata.SetAnnotation(AnnotationsConstants.ETagConcurrencyEnabledAnnotation, true);
            entityTypeBuilder.Metadata.SetAnnotation(AnnotationsConstants.ETagConcurrencyPropertyNameAnnotation, "ETag");

            return entityTypeBuilder;
        }
    }

    public static EntityTypeBuilder<TEntity> UseObjectETagConcurrency<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entityTypeBuilder);
        UseObjectETagConcurrency((EntityTypeBuilder)entityTypeBuilder);
        return entityTypeBuilder;
    }

    public static EntityTypeBuilder<TEntity> UseObjectETagConcurrency<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, string?>> etagPropertyExpression)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entityTypeBuilder);
        ArgumentNullException.ThrowIfNull(etagPropertyExpression);

        var propertyBuilder = entityTypeBuilder.Property(etagPropertyExpression).IsConcurrencyToken();

        entityTypeBuilder.Metadata.SetAnnotation(AnnotationsConstants.ETagConcurrencyEnabledAnnotation, true);
        entityTypeBuilder.Metadata.SetAnnotation(
            AnnotationsConstants.ETagConcurrencyPropertyNameAnnotation,
            propertyBuilder.Metadata.Name);

        return entityTypeBuilder;
    }

    public static ModelBuilder ApplyBlobSettingsConventions(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var blobName = GetBlobNameForEntity(entity);
            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new InvalidOperationException($"The entity {entity.Name} has no Blob name.");
            }

            entity.SetAnnotation(AnnotationsConstants.BlobNameAnnotation, blobName);
        }

        return modelBuilder;
    }

    private static string GetBlobNameForEntity(IMutableEntityType entity)
    {
        var clrType = entity.ClrType;
        var blobAttr = clrType.GetCustomAttributes(typeof(BlobSettingsAttribute), false)
            .Cast<BlobSettingsAttribute>()
            .FirstOrDefault();

        var blobName = (blobAttr != null)
            ? blobAttr.Name
            : clrType.Name;

        blobName = (blobName)
            .ToLower()
            .Trim();

        return blobName;
    }

    public static ModelBuilder Validate(this ModelBuilder modelBuilder, IStorageProvider provider)
    {
        var validator = new CloudStorageModelValidator(provider);
        validator.Validate(modelBuilder.Model);

        return modelBuilder;
    }
}