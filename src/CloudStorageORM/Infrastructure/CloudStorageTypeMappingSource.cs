namespace CloudStorageORM.Infrastructure
{
    using System.Collections.Concurrent;
    using Abstractions;
    using Microsoft.EntityFrameworkCore.Storage;
    using Options;
    using Validators;

    public class CloudStorageTypeMappingSource(
        TypeMappingSourceDependencies dependencies,
        CloudStorageOptions cloudOptions)
        : TypeMappingSource(dependencies)
    {
        private static readonly ConcurrentDictionary<Type, CoreTypeMapping> MappingsCache = new();

        protected override CoreTypeMapping? FindMapping(in TypeMappingInfo mappingInfo)
        {
            if (mappingInfo.ClrType is null)
            {
                return null;
            }

            var clrType = mappingInfo.ClrType;

            if (clrType.IsPrimitive || clrType == typeof(string) || clrType == typeof(Guid))
            {
                return base.FindMapping(mappingInfo);
            }

            if (clrType.IsAbstract || clrType.IsInterface)
            {
                return MappingsCache.GetOrAdd(mappingInfo.ClrType, CreateMapping);
            }

            var hasBlobSettings = clrType
                .GetCustomAttributes(typeof(BlobSettingsAttribute), true)
                .Any();

            if (!hasBlobSettings)
            {
                return MappingsCache.GetOrAdd(clrType, CreateMapping);
            }

            var validator = BlobValidatorFactory.Create(cloudOptions.Provider);
            var blobName = clrType.Name.ToLower().Trim();
            return !validator.IsBlobNameValid(blobName)
                ? throw new InvalidOperationException($"Entity '{clrType.Name}' must define a valid Blob Name. It has tried to use {blobName}, but it's not valid for {cloudOptions.Provider:G}.")
                : MappingsCache.GetOrAdd(clrType, CreateMapping);
        }

        private static CoreTypeMapping CreateMapping(Type clrType)
        {
            return new CloudStorageTypeMapping(clrType);
        }
    }
}
