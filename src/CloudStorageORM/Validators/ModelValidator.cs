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

        public void Validate(IModel model)
        {
            foreach (var entity in model.GetEntityTypes())
            {
                var clrType = entity.ClrType;

                var attributes = clrType.GetCustomAttributes(typeof(BlobSettingsAttribute), false)
                                        .Cast<ModelAttribute>()
                                        .ToList();

                foreach (var attribute in attributes)
                {
                    if (attribute is BlobSettingsAttribute blobSettings)
                    {
                        ValidateBlobSettings(blobSettings, entity);
                    }
                }
            }
        }

        private void ValidateBlobSettings(BlobSettingsAttribute attribute, IEntityType entity)
        {
            if (!BlobValidator.IsBlobNameValid(entity.Name))
            {
                throw new InvalidOperationException($"Invalid blob name '{entity.Name}' for entity type '{entity.ClrType.Name}'.");
            }
        }
    }
}
