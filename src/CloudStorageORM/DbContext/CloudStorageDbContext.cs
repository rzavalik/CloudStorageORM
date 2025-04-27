using CloudStorageORM.Options;
using CloudStorageORM.Repositories;

namespace CloudStorageORM.DbContext
{
    public class CloudStorageDbContext
    {
        private readonly CloudStorageOptions _options;

        public CloudStorageDbContext(CloudStorageOptions options)
        {
            _options = options;
        }

        public CloudStorageRepository<TEntity> Set<TEntity>() where TEntity : class
        {
            return new CloudStorageRepository<TEntity>();
        }

        public Task<int> SaveChangesAsync()
        {
            return Task.FromResult(0);
        }
    }
}
