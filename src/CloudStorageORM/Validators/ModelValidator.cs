using CloudStorageORM.Interfaces.Validators;

namespace CloudStorageORM.Validators
{
    using System;
    using CloudStorageORM.Abstractions;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;

    public sealed class ModelValidator
    {
        public ModelValidator(IBlobValidator blobValidator)
        {
            BlobValidator = blobValidator ?? throw new ArgumentNullException(nameof(blobValidator));
        }

        public IBlobValidator BlobValidator { get; set; }

        public void Validate(ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entity.ClrType;
                var attributes = clrType.GetCustomAttributes(typeof(BlobSettingsAttribute), false)
                                      .Cast<ModelAttribute>()
                                      .ToList();

                foreach (var attribute in attributes)
                {
                    if (attribute is BlobSettingsAttribute)
                    {
                        var blobSettings = (BlobSettingsAttribute)attribute;

                        // execute all validations for BlobSettingsAttribute
                        ValidateBlobSettings(blobSettings, entity);
                    }
                }
            }
        }

        private void ValidateBlobSettings(BlobSettingsAttribute attribute, IMutableEntityType entity)
        {
            if (!BlobValidator.IsBlobNameValid(entity.Name))
            {
                throw new InvalidOperationException($"Invalid blob name '{entity.Name}' for entity type '{entity.ClrType.Name}'.");
            }
        }
    }
}
