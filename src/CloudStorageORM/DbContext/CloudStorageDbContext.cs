using CloudStorageORM.Options;

namespace CloudStorageORM.DbContext
{
    public class CloudStorageDbContext
    {
        private readonly CloudStorageOptions _options;

        public CloudStorageDbContext(CloudStorageOptions options)
        {
            _options = options;
        }
    }
}
