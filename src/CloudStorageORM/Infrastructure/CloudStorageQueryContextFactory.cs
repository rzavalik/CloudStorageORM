using Microsoft.EntityFrameworkCore.Query;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// Creates query contexts used by CloudStorageORM query execution.
/// </summary>
public class CloudStorageQueryContextFactory(QueryContextDependencies dependencies) : IQueryContextFactory
{
    /// <inheritdoc />
    public QueryContext Create()
    {
        return new CloudStorageQueryContext(dependencies);
    }
}