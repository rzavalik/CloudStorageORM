namespace CloudStorageORM.Infrastructure
{
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Storage;
    using CloudStorageORM.Options;

    public class CloudStorageDatabaseProvider : IDatabaseProvider
    {
        public string Name => "CloudStorageORM";

        public bool IsConfigured(IDbContextOptions options)
        {
            return options.FindExtension<CloudStorageOrmOptionsExtension>() != null;
        }
    }
}
