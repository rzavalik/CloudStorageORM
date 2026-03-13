namespace CloudStorageORM.Infrastructure
{
    using System.Diagnostics;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    public class DbContextServicesDependencies
    {
        public DbContextServicesDependencies(
            DbContextOptions<DbContext> dbContextOptions,
            LoggerFactory loggerFactory,
            DiagnosticListener diagnosticListener)
        {
            _ = dbContextOptions;
            _ = loggerFactory;
            _ = diagnosticListener;
        }
    }
}