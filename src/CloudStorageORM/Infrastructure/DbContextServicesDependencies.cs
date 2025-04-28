namespace CloudStorageORM.Infrastructure
{
    using System.Diagnostics;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    internal class DbContextServicesDependencies
    {
        private DbContextOptions<DbContext> dbContextOptions;
        private LoggerFactory loggerFactory;
        private DiagnosticListener diagnosticListener;

        public DbContextServicesDependencies(DbContextOptions<DbContext> dbContextOptions, LoggerFactory loggerFactory, DiagnosticListener diagnosticListener)
        {
            this.dbContextOptions = dbContextOptions;
            this.loggerFactory = loggerFactory;
            this.diagnosticListener = diagnosticListener;
        }
    }
}