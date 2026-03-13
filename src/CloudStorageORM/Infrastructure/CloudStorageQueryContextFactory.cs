using Microsoft.EntityFrameworkCore.Query;

namespace CloudStorageORM.Infrastructure;

public class CloudStorageQueryContextFactory(QueryContextDependencies dependencies) : IQueryContextFactory
{
    public QueryContext Create()
    {
        return new CloudStorageQueryContext(dependencies);
    }
}
