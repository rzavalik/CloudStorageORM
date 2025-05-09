namespace CloudStorageORM.Validators
{
    using System;
    using System.Text.Json;
    using CloudStorageORM.Abstractions;
    using CloudStorageORM.Interfaces.Infrastructure;
    using CloudStorageORM.Interfaces.Validators;
    using Microsoft.EntityFrameworkCore.Metadata;

    public sealed class ModelValidator
    {
        private readonly IBlobPathResolver _blobPathResolver;

        public ModelValidator(
            IBlobValidator blobValidator,
            IBlobPathResolver blobPathResolver)
        {
            BlobValidator = blobValidator ?? throw new ArgumentNullException(nameof(blobValidator));
            _blobPathResolver = blobPathResolver ?? throw new ArgumentNullException(nameof(blobPathResolver));
        }

        public IBlobValidator BlobValidator { get; set; }

        public void Validate(IMutableModel model)
        {
            foreach (var entity in model.GetEntityTypes())
            {
                var clrType = entity.ClrType;

                var attributes = clrType.GetCustomAttributes(typeof(BlobSettingsAttribute), false)
                                        .Cast<ModelAttribute>()
                                        .ToList();

                // check each attribute per entity
                foreach (var attribute in attributes)
                {
                    if (attribute is BlobSettingsAttribute blobSettings)
                    {
                        ValidateBlobName(blobSettings.Name, entity);
                    }
                }

                //if the blob name is not defined by attribute, its defined by default
                if (!attributes.Any(att => string.IsNullOrEmpty((att as BlobSettingsAttribute)?.Name)))
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

        private void ValidateHasKey(IMutableEntityType entity)
        {
            if (entity.FindPrimaryKey() == null)
            {
                throw new InvalidOperationException($"Entity type '{entity.ClrType.Name}' does not have a primary key defined.");
            }
        }

        private void ValidateSerializable(IMutableEntityType entity)
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
