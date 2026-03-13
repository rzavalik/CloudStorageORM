namespace CloudStorageORM.Validators
{
    using System.Text.Json;
    using Abstractions;
    using Interfaces.Infrastructure;
    using Interfaces.Validators;
    using Microsoft.EntityFrameworkCore.Metadata;

    public sealed class ModelValidator(
        IBlobValidator blobValidator,
        IBlobPathResolver blobPathResolver)
    {
        private readonly IBlobPathResolver _blobPathResolver = blobPathResolver ?? throw new ArgumentNullException(nameof(blobPathResolver));

        private IBlobValidator BlobValidator { get; set; } = blobValidator ?? throw new ArgumentNullException(nameof(blobValidator));

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
                throw new InvalidOperationException($"Invalid blob name '{entity.Name}' for entity type '{entity.ClrType.Name}'.");
            }
        }

        private static void ValidateHasKey(IMutableEntityType entity)
        {
            if (entity.FindPrimaryKey() == null)
            {
                throw new InvalidOperationException($"Entity type '{entity.ClrType.Name}' does not have a primary key defined.");
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
}
