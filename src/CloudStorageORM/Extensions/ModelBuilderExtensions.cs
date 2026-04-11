using System.Linq.Expressions;
using CloudStorageORM.Abstractions;
using CloudStorageORM.Constants;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudStorageORM.Extensions;

/// <summary>
/// Extensions for applying CloudStorageORM model conventions and validation.
/// </summary>
public static class ModelBuilderExtensions
{
    extension(EntityTypeBuilder entityTypeBuilder)
    {
        /// <summary>
        /// Marks the default <c>ETag</c> shadow property as a concurrency token for the entity type.
        /// </summary>
        /// <returns>The same <see cref="EntityTypeBuilder" /> for chaining.</returns>
        /// <example>
        /// <code>
        /// modelBuilder.Entity&lt;User&gt;().UseObjectETagConcurrency();
        /// </code>
        /// </example>
        public EntityTypeBuilder UseObjectETagConcurrency()
        {
            ArgumentNullException.ThrowIfNull(entityTypeBuilder);

            entityTypeBuilder.Property<string?>("ETag").IsConcurrencyToken();
            entityTypeBuilder.Metadata.SetAnnotation(AnnotationsConstants.ETagConcurrencyEnabledAnnotation, true);
            entityTypeBuilder.Metadata.SetAnnotation(AnnotationsConstants.ETagConcurrencyPropertyNameAnnotation, "ETag");

            return entityTypeBuilder;
        }
    }

    /// <summary>
    /// Marks the default <c>ETag</c> shadow property as a concurrency token for the entity type.
    /// </summary>
    /// <typeparam name="TEntity">Entity type being configured.</typeparam>
    /// <param name="entityTypeBuilder">Entity type builder.</param>
    /// <returns>The same typed <see cref="EntityTypeBuilder{TEntity}" /> for chaining.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;User&gt;().UseObjectETagConcurrency();
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TEntity> UseObjectETagConcurrency<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entityTypeBuilder);
        UseObjectETagConcurrency((EntityTypeBuilder)entityTypeBuilder);
        return entityTypeBuilder;
    }

    /// <summary>
    /// Marks a specific string property as the concurrency token used for object ETag checks.
    /// </summary>
    /// <typeparam name="TEntity">Entity type being configured.</typeparam>
    /// <param name="entityTypeBuilder">Entity type builder.</param>
    /// <param name="etagPropertyExpression">Expression selecting the property that stores the ETag value.</param>
    /// <returns>The same typed <see cref="EntityTypeBuilder{TEntity}" /> for chaining.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;User&gt;().UseObjectETagConcurrency(x =&gt; x.ETag);
    /// </code>
    /// </example>
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

    /// <summary>
    /// Applies BlobSettings conventions by computing and storing blob-name annotations for every entity.
    /// </summary>
    /// <param name="modelBuilder">Model builder instance.</param>
    /// <returns>The same <see cref="ModelBuilder" /> for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an entity resolves to an empty blob name.</exception>
    /// <example>
    /// <code>
    /// modelBuilder.ApplyBlobSettingsConventions();
    /// </code>
    /// </example>
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

    /// <summary>
    /// Validates the EF model against provider-specific CloudStorageORM rules.
    /// </summary>
    /// <param name="modelBuilder">Model builder instance.</param>
    /// <param name="provider">Storage provider used to choose the proper validation rules.</param>
    /// <returns>The same <see cref="ModelBuilder" /> for chaining.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Validate(storageProvider);
    /// </code>
    /// </example>
    public static ModelBuilder Validate(this ModelBuilder modelBuilder, IStorageProvider provider)
    {
        var validator = new CloudStorageModelValidator(provider);
        validator.Validate(modelBuilder.Model);

        return modelBuilder;
    }
}