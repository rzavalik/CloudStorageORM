namespace CloudStorageORM.Interfaces.StorageProviders
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IStorageProvider
    {
        Task SaveAsync<T>(string path, T entity);
        Task<T> ReadAsync<T>(string path);
        Task DeleteAsync(string path);
        Task<List<string>> ListAsync(string folderPath);
    }
}