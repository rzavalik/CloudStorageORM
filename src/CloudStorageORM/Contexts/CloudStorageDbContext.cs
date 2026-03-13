using CloudStorageORM.Extensions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Providers;
using CloudStorageORM.Validators;
using Microsoft.EntityFrameworkCore;

namespace CloudStorageORM.Contexts;

public class CloudStorageDbContext : DbContext
{
    private readonly IStorageProvider _storageProvider;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyBlobSettingsConventions();

        var validator = new CloudStorageModelValidator(_storageProvider);

        validator.Validate(modelBuilder.Model);
    }
}