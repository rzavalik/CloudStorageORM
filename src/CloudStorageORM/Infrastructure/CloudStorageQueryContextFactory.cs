namespace CloudStorageORM.Infrastructure
{
    using Microsoft.EntityFrameworkCore.Query;

    public class CloudStorageQueryContextFactory : IQueryContextFactory
    {
        private readonly QueryContextDependencies _dependencies;

        public CloudStorageQueryContextFactory(QueryContextDependencies dependencies)
        {
            _dependencies = dependencies;
        }

        public QueryContext Create()
        {
            return new CloudStorageQueryContext(_dependencies);
        }
    }
}
