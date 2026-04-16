using CloudStorageORM.Extensions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Observability;
using CloudStorageORM.Providers;
using CloudStorageORM.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace CloudStorageORM.Contexts;

/// <summary>
/// Base DbContext implementation for CloudStorageORM-backed models.
/// </summary>
public class CloudStorageDbContext : DbContext
{
    private readonly IStorageProvider _storageProvider;
    private readonly bool _enableLogging;

    /// <summary>
    /// Creates a CloudStorageORM DbContext from configured EF options.
    /// </summary>
    /// <param name="options">DbContext options containing a configured CloudStorageORM extension.</param>
    /// <exception cref="InvalidCastException">Thrown when CloudStorageORM options are missing from the provided EF options.</exception>
    /// <example>
    /// <code>
    /// var options = new DbContextOptionsBuilder&lt;MyDbContext&gt;()
    ///     .UseCloudStorageOrm(o =&gt;
    ///     {
    ///         o.Provider = CloudStorageORM.Enums.CloudProvider.Azure;
    ///         o.ContainerName = "app-data";
    ///         o.Azure.ConnectionString = "UseDevelopmentStorage=true";
    ///     })
    ///     .Options;
    /// </code>
    /// </example>
    public CloudStorageDbContext(DbContextOptions options)
        : base(options)
    {
        var options1 = options
                           .Extensions
                           .OfType<CloudStorageOrmOptionsExtension>()
                           .FirstOrDefault()
                           ?.Options
                       ?? throw new InvalidCastException("Options must be of type CloudStorageOptions.");

        var provider = options1.Provider;
        var containerName = options1.ContainerName;
        _enableLogging = options1.Observability.EnableLogging;

        _storageProvider = ProviderFactory.GetStorageProvider(options1)
                           ?? throw new ArgumentNullException(nameof(_storageProvider));

        if (!_enableLogging)
        {
            return;
        }

        var logger = TryResolveLogger();
        logger?.LogConfigurationInitialized(provider.ToString(), containerName);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var logger = _enableLogging ? TryResolveLogger() : null;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        modelBuilder.ApplyBlobSettingsConventions();

        var validator = new CloudStorageModelValidator(_storageProvider);
        logger?.LogValidationStarting("CloudStorageModel");

        try
        {
            validator.Validate(modelBuilder.Model);
            stopwatch.Stop();
            logger?.LogValidationCompleted("CloudStorageModel", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger?.LogValidationFailed("CloudStorageModel", ex.Message, ex);
            throw;
        }
    }

    private ILogger<CloudStorageDbContext>? TryResolveLogger()
    {
        return ((IInfrastructure<IServiceProvider>)this).Instance.GetService(typeof(ILogger<CloudStorageDbContext>))
            as ILogger<CloudStorageDbContext>;
    }
}