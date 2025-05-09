namespace CloudStorageORM.Interfaces.StorageProviders
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CloudStorageORM.Enums;

    public interface IStorageProvider
    {
        CloudProvider CloudProvider { get; }
        Task DeleteContainerAsync();
        Task CreateContainerIfNotExistsAsync();
        Task SaveAsync<T>(string path, T entity);
        Task<T> ReadAsync<T>(string path);
        Task DeleteAsync(string path);
        Task<List<string>> ListAsync(string folderPath);
        string SanitizeBlobName(string rawName);
    }
}