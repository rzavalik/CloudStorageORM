namespace CloudStorageORM.DbContext
{
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Repositories;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System;

    public class CloudStorageDbContext : DbContext
    {
        private readonly IStorageProvider _storageProvider;
        private readonly Dictionary<Type, object> _repositories = new();

        public CloudStorageDbContext(DbContextOptions<CloudStorageDbContext> options)
            : base(options)
        {
        }

        public new CloudStorageRepository<TEntity> Set<TEntity>() where TEntity : class
        {
            if (!_repositories.TryGetValue(typeof(TEntity), out var repository))
            {
                repository = new CloudStorageRepository<TEntity>(_storageProvider);
                _repositories[typeof(TEntity)] = repository;
            }

            return (CloudStorageRepository<TEntity>)repository;
        }

        public new void Add<TEntity>(TEntity entity) where TEntity : class
        {
            var repository = Set<TEntity>();
            repository.AddAsync(Guid.NewGuid().ToString(), entity).Wait();
        }

        public new void Update<TEntity>(TEntity entity) where TEntity : class
        {
            var repository = Set<TEntity>();
            repository.UpdateAsync(Guid.NewGuid().ToString(), entity).Wait();
        }

        public new void Remove<TEntity>(TEntity entity) where TEntity : class
        {
            var repository = Set<TEntity>();
            repository.RemoveAsync(Guid.NewGuid().ToString()).Wait();
        }

        public async Task<int> SaveChangesAsync()
        {
            // No-op for now, as changes are directly applied in Add/Update/Remove
            return await Task.FromResult(0);
        }
    }
}
