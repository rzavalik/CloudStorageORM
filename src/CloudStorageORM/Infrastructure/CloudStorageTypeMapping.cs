namespace CloudStorageORM.Infrastructure
{
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.EntityFrameworkCore.Storage.Json;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

    public class CloudStorageTypeMapping : CoreTypeMapping
    {
        public CloudStorageTypeMapping(Type clrType)
            : base(new CoreTypeMappingParameters(clrType))
        {
        }

        protected CloudStorageTypeMapping(CoreTypeMappingParameters parameters)
            : base(parameters)
        {
        }

        public override CoreTypeMapping WithComposedConverter(
            ValueConverter? converter,
            ValueComparer? comparer = null,
            ValueComparer? keyComparer = null,
            CoreTypeMapping? elementMapping = null,
            JsonValueReaderWriter? jsonValueReaderWriter = null)
        {
            return new CloudStorageTypeMapping(
                Parameters.WithComposedConverter(converter, comparer, keyComparer, elementMapping, jsonValueReaderWriter));
        }

        protected override CoreTypeMapping Clone(CoreTypeMappingParameters parameters)
        {
            return new CloudStorageTypeMapping(parameters);
        }
    }
}
