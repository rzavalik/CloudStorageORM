using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// Placeholder wrapper for DbContext service dependencies used by CloudStorageORM.
/// </summary>
public class DbContextServicesDependencies
{
    /// <summary>
    /// Initializes DbContext service dependency placeholders.
    /// </summary>
    /// <param name="dbContextOptions">DbContext options instance.</param>
    /// <param name="loggerFactory">Logger factory used by EF diagnostics.</param>
    /// <param name="diagnosticListener">Diagnostic listener used for instrumentation hooks.</param>
    /// <example>
    /// <code>
    /// var deps = new DbContextServicesDependencies(options, loggerFactory, listener);
    /// </code>
    /// </example>
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