namespace CloudStorageORM.Infrastructure
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using CloudStorageORM.Interfaces.Infrastructure;
    using CloudStorageORM.Interfaces.StorageProviders;
    using Microsoft.EntityFrameworkCore.Update;

    public class BlobPathResolver : IBlobPathResolver
    {
        private readonly IStorageProvider _storageProvider;

        public BlobPathResolver(
            IStorageProvider storageProvider)
        {
            _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        }

        public string GetBlobName(Type type)
        {
            var name = type.Name.ToLowerInvariant();
            var hashSource = GetFullTypeName(type);
            var hash = GetDeterministicHash(hashSource);

            return $"{Sanitize(hash)}-{Sanitize(name)}";
        }

        private static string GetFullTypeName(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.FullName ?? type.Name;
            }

            var genericTypeName = type.GetGenericTypeDefinition().FullName ?? type.Name;
            var genericArgs = string.Join(",", type.GetGenericArguments().Select(GetFullTypeName));
            return $"{genericTypeName}<{genericArgs}>";
        }

        private static string GetDeterministicHash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);

            return BitConverter
                .ToString(hash)
                .Replace("-", "")
                .ToLowerInvariant()
                .Substring(0, 16);
        }

        private string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _storageProvider.SanitizeBlobName(SanitizeLocally(name));
        }

        private static string SanitizeLocally(string name)
        {
            return name
                .ToLower()
                .Replace(".", "_")
                .Replace("+", "_")
                .Replace("`", "_")
                .Replace("[", "_")
                .Replace("]", "_");
        }

        public string GetPath(Type entity)
        {
            var blobName = GetBlobName(entity);
            return $"{blobName}/";
        }

        public string GetPath(IUpdateEntry entry)
        {
            var keyProperty = entry.EntityType.FindPrimaryKey()?.Properties.FirstOrDefault();
            var keyValue = entry.GetCurrentValue(keyProperty!);

            if (string.IsNullOrWhiteSpace(keyValue?.ToString()))
            {
                throw new InvalidOperationException($"Cannot persist entity '{entry.EntityType.Name}' without a valid key value.");
            }

            return GetPath(entry.EntityType.ClrType, keyValue);
        }

        public string GetPath(Type entity, object id)
        {
            var blobName = GetBlobName(entity);
            return $"{blobName}/{id}.json";
        }
    }
}
