namespace CloudStorageORM.Extensions
{
    using System.Collections.Generic;
    using CloudStorageORM.Infrastructure;
    using Microsoft.EntityFrameworkCore.Infrastructure;

    public class CloudStorageOrmOptionsExtensionInfo : DbContextOptionsExtensionInfo
    {
        public CloudStorageOrmOptionsExtensionInfo(
            CloudStorageOrmOptionsExtension extension)
            : base(extension)
        {
        }

        public override string LogFragment => $"CloudStorageORM: {Extension.Options.ConnectionString}";

        public override bool IsDatabaseProvider => true;

        public CloudStorageOrmOptionsExtension Extension => (CloudStorageOrmOptionsExtension)base.Extension;

        public override int GetServiceProviderHashCode() => Extension.Options.ConnectionString?.GetHashCode() ?? 0;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
        {
            return false;
        }
    }
}
