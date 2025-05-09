namespace CloudStorageORM.Infrastructure
{
    using System;
    using System.Linq;
    using CloudStorageORM.Abstractions;
    using CloudStorageORM.Interfaces.Infrastructure;
    using Microsoft.EntityFrameworkCore.Update;

    public class BlobPathResolver : IBlobPathResolver
    {
        public string GetBlobName(Type type)
        {
            var blobAttr = type.GetCustomAttributes(typeof(BlobSettingsAttribute), false)
                               .Cast<BlobSettingsAttribute>()
                               .FirstOrDefault();

            return blobAttr?.Name ?? type.Name.ToLowerInvariant().Trim();
        }

        public string GetPath(IUpdateEntry entry)
        {
            var blobName = GetBlobName(entry.EntityType.ClrType);
            var keyProperty = entry.EntityType.FindPrimaryKey()?.Properties.FirstOrDefault();
            var keyValue = entry.GetCurrentValue(keyProperty!);

            if (string.IsNullOrWhiteSpace(keyValue?.ToString()))
            {
                throw new InvalidOperationException($"Cannot persist entity '{entry.EntityType.Name}' without a valid key value.");
            }

            return $"{blobName}/{keyValue}.json";
        }
    }
}
