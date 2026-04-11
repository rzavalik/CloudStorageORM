using CloudStorageORM.Abstractions;
using CloudStorageORM.Enums;

namespace CloudStorageORM.Interfaces.StorageProviders;

public interface IStorageProvider
{
    CloudProvider CloudProvider { get; }
    Task DeleteContainerAsync();
    Task CreateContainerIfNotExistsAsync();
    Task SaveAsync<T>(string path, T entity);
    Task<string?> SaveAsync<T>(string path, T entity, string? ifMatchETag);
    Task<T> ReadAsync<T>(string path);
    Task<StorageObject<T>> ReadWithMetadataAsync<T>(string path);
    Task DeleteAsync(string path);
    Task DeleteAsync(string path, string? ifMatchETag);
    Task<List<string>> ListAsync(string folderPath);
    string SanitizeBlobName(string rawName);
}