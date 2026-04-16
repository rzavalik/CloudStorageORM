using Microsoft.EntityFrameworkCore.Query;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// CloudStorageORM-specific query context that reuses EF Core query infrastructure.
/// </summary>
public class CloudStorageQueryContext(QueryContextDependencies dependencies) : QueryContext(dependencies);