using Microsoft.EntityFrameworkCore.Query;

namespace CloudStorageORM.Infrastructure;

public class CloudStorageQueryContext(QueryContextDependencies dependencies) : QueryContext(dependencies);