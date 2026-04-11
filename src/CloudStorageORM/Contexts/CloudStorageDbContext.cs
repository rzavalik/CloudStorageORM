using CloudStorageORM.Extensions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Providers;
using CloudStorageORM.Validators;
using Microsoft.EntityFrameworkCore;

namespace CloudStorageORM.Contexts;

/// <summary>
/// Base DbContext implementation for CloudStorageORM-backed models.
/// </summary>
public class CloudStorageDbContext : DbContext
{
    private readonly IStorageProvider _storageProvider;

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

        _storageProvider = ProviderFactory.GetStorageProvider(options1)
                           ?? throw new ArgumentNullException(nameof(_storageProvider));
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyBlobSettingsConventions();

        var validator = new CloudStorageModelValidator(_storageProvider);

        validator.Validate(modelBuilder.Model);
    }
}