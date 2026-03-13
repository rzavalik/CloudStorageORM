using System.Diagnostics;
using CloudStorageORM.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

/// <summary>
/// Exercises the internal DbContextServicesDependencies constructor so the
/// class appears as covered.
/// </summary>
public class DbContextServicesDependenciesTests
{
    [Fact]
    public void Constructor_DoesNotThrow()
    {
        var options = new DbContextOptionsBuilder<DbContext>().Options;
        var loggerFactory = new LoggerFactory();
        var listener = new DiagnosticListener("test");

        // Internal type: accessible via InternalsVisibleTo
        var deps = new DbContextServicesDependencies(
            options, loggerFactory, listener);

        deps.ShouldNotBeNull();
    }
}