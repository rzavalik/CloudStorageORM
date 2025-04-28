namespace CloudStorageORM.Repositories
{
    using CloudStorageORM.Interfaces.StorageProviders;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class CloudStorageRepository<TEntity> : DbSet<TEntity> where TEntity : class
    {
        private readonly IStorageProvider _storageProvider;
        private readonly string _folderName;

        public override IEntityType EntityType => throw new NotSupportedException("Custom metadata is not supported in this implementation.");

        public CloudStorageRepository(IStorageProvider storageProvider)
        {
            _storageProvider = storageProvider;
            _folderName = typeof(TEntity).Name.ToLowerInvariant();
        }

        public async Task AddAsync(string id, TEntity entity)
        {
            var path = $"{_folderName}/{id}.json";

            var existing = await _storageProvider.ReadAsync<TEntity>(path);
            if (existing != null)
            {
                throw new System.Exception($"Entity with id '{id}' already exists.");
            }

            await _storageProvider.SaveAsync(path, entity);
        }

        public async Task UpdateAsync(string id, TEntity entity)
        {
            var path = $"{_folderName}/{id}.json";

            var existing = await _storageProvider.ReadAsync<TEntity>(path);
            if (existing == null)
            {
                throw new System.Exception($"Entity with id '{id}' does not exist.");
            }

            await _storageProvider.SaveAsync(path, entity);
        }

        public async Task<TEntity> FindAsync(string id)
        {
            var path = $"{_folderName}/{id}.json";
            return await _storageProvider.ReadAsync<TEntity>(path);
        }

        public async Task<List<TEntity>> ListAsync()
        {
            var entityPaths = await _storageProvider.ListAsync(_folderName);

            var list = new List<TEntity>();

            foreach (var path in entityPaths)
            {
                var entity = await _storageProvider.ReadAsync<TEntity>(path);
                if (entity != null)
                {
                    list.Add(entity);
                }
            }

            return list;
        }

        public async Task RemoveAsync(string id)
        {
            var path = $"{_folderName}/{id}.json";
            await _storageProvider.DeleteAsync(path);
        }
    }
}
