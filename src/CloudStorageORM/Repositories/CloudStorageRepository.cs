using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CloudStorageORM.Repositories;

public class CloudStorageRepository<TEntity>(IStorageProvider storageProvider) : DbSet<TEntity>
    where TEntity : class
{
    private readonly string _folderName = typeof(TEntity).Name.ToLowerInvariant();

    public override IEntityType EntityType => throw new NotSupportedException("Custom metadata is not supported in this implementation.");

    public async Task AddAsync(string id, TEntity entity)
    {
        var path = $"{_folderName}/{id}.json";

        var existing = await storageProvider.ReadAsync<TEntity>(path);
        if (existing != null)
        {
            throw new Exception($"Entity with id '{id}' already exists.");
        }

        await storageProvider.SaveAsync(path, entity);
    }

    public async Task UpdateAsync(string id, TEntity entity)
    {
        var path = $"{_folderName}/{id}.json";

        var existing = await storageProvider.ReadAsync<TEntity>(path);
        if (existing == null)
        {
            throw new Exception($"Entity with id '{id}' does not exist.");
        }

        await storageProvider.SaveAsync(path, entity);
    }

    public async Task<TEntity> FindAsync(string id)
    {
        var path = $"{_folderName}/{id}.json";
        return await storageProvider.ReadAsync<TEntity>(path);
    }

    public async Task<List<TEntity>> ListAsync()
    {
        var entityPaths = await storageProvider.ListAsync(_folderName);

        var list = new List<TEntity>();

        foreach (var path in entityPaths)
        {
            var entity = await storageProvider.ReadAsync<TEntity>(path);
            list.Add(entity);
        }

        return list;
    }

    public async Task RemoveAsync(string id)
    {
        var path = $"{_folderName}/{id}.json";
        await storageProvider.DeleteAsync(path);
    }
}
