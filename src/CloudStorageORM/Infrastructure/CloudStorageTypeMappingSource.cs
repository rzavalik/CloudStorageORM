namespace CloudStorageORM.Infrastructure
{
    using System.Collections.Concurrent;
    using Microsoft.EntityFrameworkCore.Storage;

    public class CloudStorageTypeMappingSource : TypeMappingSource
    {
        private static readonly ConcurrentDictionary<Type, CoreTypeMapping> _mappingsCache = new();

        public CloudStorageTypeMappingSource(TypeMappingSourceDependencies dependencies)
            : base(dependencies)
        {
        }

        protected override CoreTypeMapping? FindMapping(in TypeMappingInfo mappingInfo)
        {
            if (mappingInfo.ClrType is null)
                return null;

            return _mappingsCache.GetOrAdd(mappingInfo.ClrType, CreateMapping);
        }

        private CoreTypeMapping CreateMapping(Type clrType)
        {
            return new CloudStorageTypeMapping(clrType);
        }
    }
}
