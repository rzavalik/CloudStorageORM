using CloudStorageORM.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CloudStorageORM.Extensions;

public class CloudStorageOrmOptionsExtensionInfo(CloudStorageOrmOptionsExtension extension)
    : DbContextOptionsExtensionInfo(extension)
{
    public override string LogFragment => $"CloudStorageORM: {Extension.Options.ConnectionString}";

    public override bool IsDatabaseProvider => true;

    public new CloudStorageOrmOptionsExtension Extension => (CloudStorageOrmOptionsExtension)base.Extension;

    public override int GetServiceProviderHashCode() => Extension.Options.ConnectionString.GetHashCode();

    public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
    {
    }

    public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
    {
        return false;
    }
}
