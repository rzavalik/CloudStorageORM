namespace CloudStorageORM.Infrastructure
{
    using Microsoft.EntityFrameworkCore.Query;

    public class CloudStorageQueryContext : QueryContext
    {
        public CloudStorageQueryContext(QueryContextDependencies dependencies)
            : base(dependencies)
        {
        }
    }
}