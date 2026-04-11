using System.Text.Json;
using CloudStorageORM.Abstractions;
using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.Validators;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CloudStorageORM.Validators;

/// <summary>
/// Validates entity model metadata such as blob naming, keys, and JSON serializability.
/// </summary>
/// <param name="blobValidator">Blob-name validator used to enforce provider naming rules.</param>
/// <param name="blobPathResolver">Path resolver used for default blob-name resolution.</param>
public sealed class ModelValidator(
    IBlobValidator blobValidator,
    IBlobPathResolver blobPathResolver)
{
    private readonly IBlobPathResolver _blobPathResolver =
        blobPathResolver ?? throw new ArgumentNullException(nameof(blobPathResolver));

    private IBlobValidator BlobValidator { get; set; } =
        blobValidator ?? throw new ArgumentNullException(nameof(blobValidator));

    /// <summary>
    /// Validates each entity mapped in the provided EF model.
    /// </summary>
    /// <param name="model">Mutable model containing entity metadata to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when an entity has an invalid blob name, missing key, or is not serializable.</exception>
    /// <example>
    /// <code>
    /// var validator = new ModelValidator(blobValidator, blobPathResolver);
    /// validator.Validate(modelBuilder.Model);
    /// </code>
    /// </example>
    public void Validate(IMutableModel model)
    {
        foreach (var entity in model.GetEntityTypes())
        {
            var clrType = entity.ClrType;

            var blobSettingsAttributes = clrType.GetCustomAttributes(typeof(BlobSettingsAttribute), false)
                .Cast<BlobSettingsAttribute>()
                .ToList();

            // check each attribute per entity
            foreach (var blobSettings in blobSettingsAttributes)
            {
                ValidateBlobName(blobSettings.Name, entity);
            }

            // if the blob name is not defined by attribute, it's defined by default
            if (!blobSettingsAttributes.Any(att => string.IsNullOrEmpty(att.Name)))
            {
                var blobName = _blobPathResolver.GetBlobName(clrType);
                ValidateBlobName(blobName, entity);
            }

            ValidateHasKey(entity);
            ValidateSerializable(entity);
        }
    }

    private void ValidateBlobName(string? blobName, IMutableEntityType entity)
    {
        if (!BlobValidator.IsBlobNameValid(blobName))
        {
            throw new InvalidOperationException(
                $"Invalid blob name '{entity.Name}' for entity type '{entity.ClrType.Name}'.");
        }
    }

    private static void ValidateHasKey(IMutableEntityType entity)
    {
        if (entity.FindPrimaryKey() == null)
        {
            throw new InvalidOperationException(
                $"Entity type '{entity.ClrType.Name}' does not have a primary key defined.");
        }
    }

    private static void ValidateSerializable(IMutableEntityType entity)
    {
        try
        {
            var type = entity.ClrType;

            var instance = Activator.CreateInstance(type);
            var json = JsonSerializer.Serialize(instance);
            _ = JsonSerializer.Deserialize(json, type);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Entity type '{entity.ClrType.FullName}' is not serializable with System.Text.Json. Exception: {ex.Message}",
                ex);
        }
    }
}