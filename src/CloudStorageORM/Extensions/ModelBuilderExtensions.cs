namespace CloudStorageORM.Extensions
{
    using CloudStorageORM.Abstractions;
    using CloudStorageORM.Enums;
    using CloudStorageORM.Validators;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;

    public static class ModelBuilderExtensions
    {
        public static ModelBuilder ApplyBlobSettingsConventions(this ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var blobName = GetBlobNameForEntity(entity);
                if (string.IsNullOrWhiteSpace(blobName))
                {
                    throw new InvalidOperationException($"The entity {entity.Name} has no Blob name.");
                }

                entity.SetAnnotation("CloudStorageORM:BlobName", blobName);
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

            blobName = (blobName ?? clrType.Name)
                .ToLower()
                .Trim();

            return blobName;
        }

        public static ModelBuilder Validate(this ModelBuilder modelBuilder, CloudProvider provider)
        {
            var validator = new CloudStorageModelValidator(provider);
            validator.Validate(modelBuilder.Model);

            return modelBuilder;
        }
    }
}