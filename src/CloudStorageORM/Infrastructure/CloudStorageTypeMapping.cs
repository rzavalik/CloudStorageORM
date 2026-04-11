using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// Type mapping used by CloudStorageORM for EF Core value conversion services.
/// </summary>
public class CloudStorageTypeMapping : CoreTypeMapping
{
    /// <summary>
    /// Creates a new mapping for the specified CLR type.
    /// </summary>
    /// <param name="clrType">CLR type to map.</param>
    public CloudStorageTypeMapping(Type clrType)
        : base(new CoreTypeMappingParameters(clrType))
    {
    }

    protected CloudStorageTypeMapping(CoreTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <inheritdoc />
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