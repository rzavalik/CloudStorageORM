namespace CloudStorageORM.Interfaces.Repositories
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ICloudStorageRepository<TEntity> where TEntity : class
    {
        Task AddAsync(string id, TEntity entity);
        Task UpdateAsync(string id, TEntity entity);
        Task<TEntity> FindAsync(string id);
        Task<List<TEntity>> ListAsync();
        Task RemoveAsync(string id);
    }
}
