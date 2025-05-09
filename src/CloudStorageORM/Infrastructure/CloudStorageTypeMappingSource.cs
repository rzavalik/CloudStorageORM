namespace CloudStorageORM.Infrastructure
{
    using System.Collections.Concurrent;
    using CloudStorageORM.Abstractions;
    using CloudStorageORM.Options;
    using CloudStorageORM.Validators;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.Options;

    public class CloudStorageTypeMappingSource : TypeMappingSource
    {
        private static readonly ConcurrentDictionary<Type, CoreTypeMapping> _mappingsCache = new();
        private readonly CloudStorageOptions _options;

        public CloudStorageTypeMappingSource(
            TypeMappingSourceDependencies dependencies,
            CloudStorageOptions cloudOptions)
                    : base(dependencies)
        {
            _options = cloudOptions;
        }

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

            if (clrType != null && !clrType.IsAbstract && !clrType.IsInterface)
            {
                var hasBlobSettings = clrType
                    .GetCustomAttributes(typeof(BlobSettingsAttribute), true)
                    .Any();

                if (hasBlobSettings)
                {
                    var validator = BlobValidatorFactory.Create(_options.Provider);
                    var blobName = clrType.Name.ToLower().Trim();
                    if (!validator.IsBlobNameValid(blobName))
                    {
                        throw new InvalidOperationException($"Entity '{clrType.Name}' must define a valid Blob Name. It has tried to use {blobName}, but it's not valid for {_options.Provider:G}.");
                    }
                }

                return _mappingsCache.GetOrAdd(clrType, CreateMapping);
            }

            return _mappingsCache.GetOrAdd(mappingInfo.ClrType, CreateMapping);
        }

        private CoreTypeMapping CreateMapping(Type clrType)
        {
            return new CloudStorageTypeMapping(clrType);
        }
    }
}
