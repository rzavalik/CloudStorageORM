namespace CloudStorageORM.DbContext
{
    using System;
    using System.Collections.Generic;
    using CloudStorageORM.Extensions;
    using CloudStorageORM.Infrastructure;
    using CloudStorageORM.Interfaces.Infrastructure;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using CloudStorageORM.Repositories;
    using CloudStorageORM.Validators;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
    using Microsoft.EntityFrameworkCore.Storage;

    public class CloudStorageDbContext : DbContext
    {
        private CloudStorageOptions? _options;
        private IStorageProvider? _storageProvider;
        private IBlobPathResolver? _blobPathResolver;
        private Dictionary<Type, object>? _repositories;
        private readonly Dictionary<Type, object> _dbSets = new();

        public CloudStorageDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public override DbSet<TEntity> Set<TEntity>()
        {
            if (!_dbSets.TryGetValue(typeof(TEntity), out var set))
            {
                var provider = this.GetService<IStorageProvider>();
                var database = this.GetService<IDatabase>() as CloudStorageDatabase
                    ?? throw new InvalidOperationException("Missing CloudStorageDatabase.");
                var blobPathResolver = new BlobPathResolver(provider);

                set = (DbSet<TEntity>)Activator.CreateInstance(
                    typeof(CloudStorageDbSet<>).MakeGenericType(typeof(TEntity)),
                    provider,
                    blobPathResolver,
                    new CurrentDbContext(this),
                    database
                )!;
                _dbSets[typeof(TEntity)] = set;
            }

            return (DbSet<TEntity>)set;
        }

        protected CloudStorageOptions Options =>
            _options ??= this.GetService<CloudStorageOptions>()
                ?? throw new InvalidOperationException("CloudStorageOptions not found in DI.");

        protected IStorageProvider StorageProvider =>
            _storageProvider ??= this.GetService<IStorageProvider>()
                ?? throw new InvalidOperationException("IStorageProvider not found in DI.");

        protected IBlobPathResolver BlobPathResolver =>
            _blobPathResolver ??= this.GetService<IBlobPathResolver>()
                ?? throw new InvalidOperationException("IBlobPathResolver not found in DI.");

        protected Dictionary<Type, object> Repositories =>
            _repositories ??= new Dictionary<Type, object>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyBlobSettingsConventions();

            var validator = new CloudStorageModelValidator(StorageProvider);
            validator.Validate(modelBuilder.Model);
        }
    }
}
