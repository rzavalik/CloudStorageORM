namespace CloudStorageORM.Extensions
{
    using CloudStorageORM.Abstractions;
    using Microsoft.EntityFrameworkCore;

    public static class ModelBuilderExtensions
    {
        public static ModelBuilder ApplyBlobSettingsConventions(this ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyBlobSettingsName();

            return modelBuilder;
        }

        private static ModelBuilder ApplyBlobSettingsName(this ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
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
                
                entity.SetAnnotation("CloudStorageORM:BlobName", blobName);
            }

            return modelBuilder;
        }
    }
}