using CloudStorageORM.Interfaces.Validators;

namespace CloudStorageORM.Validators
{
    using System;
    using CloudStorageORM.Abstractions;
    using Microsoft.EntityFrameworkCore.Metadata;

    public sealed class ModelValidator
    {
        public ModelValidator(IBlobValidator blobValidator)
        {
            BlobValidator = blobValidator ?? throw new ArgumentNullException(nameof(blobValidator));
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

                // check generic / global requirements
                var blobName = entity.GetAnnotation("CloudStorageORM:BlobName")?.Value as string;
                ValidateBlobName(blobName, entity);
                ValidateHasKey(entity);
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
    }
}
