namespace CloudStorageORM.Interfaces.Repositories;

public interface ICloudStorageRepository<TEntity> where TEntity : class
{
    Task AddAsync(string id, TEntity entity);
    Task UpdateAsync(string id, TEntity entity);
    Task<TEntity> FindAsync(string id);
    Task<List<TEntity>> ListAsync();
    Task RemoveAsync(string id);
}
