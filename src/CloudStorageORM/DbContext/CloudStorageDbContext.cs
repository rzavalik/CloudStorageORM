namespace CloudStorageORM.DbContext
{
    using System;
    using System.Collections.Generic;
    using CloudStorageORM.Extensions;
    using CloudStorageORM.Infrastructure;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using CloudStorageORM.Providers;
    using CloudStorageORM.Validators;
    using Microsoft.EntityFrameworkCore;

    public class CloudStorageDbContext : DbContext
    {
        private readonly CloudStorageOptions _options;
        private readonly IStorageProvider _storageProvider;
        private readonly Dictionary<Type, object> _repositories = new();

        public CloudStorageDbContext(DbContextOptions options)
            : base(options)
        {
            _options = options
                .Extensions
                .OfType<CloudStorageOrmOptionsExtension>()
                .FirstOrDefault()
                ?.Options
                ?? throw new InvalidCastException("Options must be of type CloudStorageOptions.");

            _storageProvider = ProviderFactory.GetStorageProvider(_options)
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
}
