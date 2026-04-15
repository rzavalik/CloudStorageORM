using CloudStorageORM.Abstractions;
using CloudStorageORM.Enums;

namespace CloudStorageORM.Interfaces.StorageProviders;

public interface IStorageProvider
{
    /// <summary>
    /// Gets the cloud provider represented by this storage implementation.
    /// </summary>
    CloudProvider CloudProvider { get; }

    /// <summary>
    /// Deletes the underlying container or bucket when it exists.
    /// </summary>
    /// <example>
    /// <code>
    /// await storageProvider.DeleteContainerAsync();
    /// </code>
    /// </example>
    Task DeleteContainerAsync();

    /// <summary>
    /// Creates the container or bucket if it does not already exist.
    /// </summary>
    /// <example>
    /// <code>
    /// await storageProvider.CreateContainerIfNotExistsAsync();
    /// </code>
    /// </example>
    Task CreateContainerIfNotExistsAsync();

    /// <summary>
    /// Persists an entity payload at the specified path without an ETag precondition.
    /// </summary>
    /// <typeparam name="T">Entity payload type to serialize.</typeparam>
    /// <param name="path">Object path inside the container.</param>
    /// <param name="entity">Entity payload to store.</param>
    /// <example>
    /// <code>
    /// await storageProvider.SaveAsync("users/42.json", user);
    /// </code>
    /// </example>
    Task SaveAsync<T>(string path, T entity);

    /// <summary>
    /// Persists an entity payload using an optional ETag precondition and returns the resulting ETag.
    /// </summary>
    /// <typeparam name="T">Entity payload type to serialize.</typeparam>
    /// <param name="path">Object path inside the container.</param>
    /// <param name="entity">Entity payload to store.</param>
    /// <param name="ifMatchETag">Expected ETag value used for optimistic concurrency.</param>
    /// <returns>The new ETag when available; otherwise <see langword="null" />.</returns>
    /// <example>
    /// <code>
    /// var newEtag = await storageProvider.SaveAsync("users/42.json", user, currentEtag);
    /// </code>
    /// </example>
    Task<string?> SaveAsync<T>(string path, T entity, string? ifMatchETag);

    /// <summary>
    /// Reads and deserializes an entity payload from object storage.
    /// </summary>
    /// <typeparam name="T">Entity payload type to deserialize.</typeparam>
    /// <param name="path">Object path inside the container.</param>
    /// <returns>The deserialized entity payload.</returns>
    /// <example>
    /// <code>
    /// var user = await storageProvider.ReadAsync&lt;User&gt;("users/42.json");
    /// </code>
    /// </example>
    Task<T> ReadAsync<T>(string path);

    /// <summary>
    /// Reads an entity payload and its metadata from object storage.
    /// </summary>
    /// <typeparam name="T">Entity payload type to deserialize.</typeparam>
    /// <param name="path">Object path inside the container.</param>
    /// <returns>A wrapper containing the entity value, ETag, and existence status.</returns>
    /// <example>
    /// <code>
    /// var result = await storageProvider.ReadWithMetadataAsync&lt;User&gt;("users/42.json");
    /// if (result.Exists) { Console.WriteLine(result.ETag); }
    /// </code>
    /// </example>
    Task<StorageObject<T>> ReadWithMetadataAsync<T>(string path);

    /// <summary>
    /// Deletes an object from storage without an ETag precondition.
    /// </summary>
    /// <param name="path">Object path inside the container.</param>
    /// <example>
    /// <code>
    /// await storageProvider.DeleteAsync("users/42.json");
    /// </code>
    /// </example>
    Task DeleteAsync(string path);

    /// <summary>
    /// Deletes an object from storage using an optional ETag precondition.
    /// </summary>
    /// <param name="path">Object path inside the container.</param>
    /// <param name="ifMatchETag">Expected ETag value used for optimistic concurrency.</param>
    /// <example>
    /// <code>
    /// await storageProvider.DeleteAsync("users/42.json", currentEtag);
    /// </code>
    /// </example>
    Task DeleteAsync(string path, string? ifMatchETag);

    /// <summary>
    /// Lists object paths under a folder-like prefix.
    /// </summary>
    /// <param name="folderPath">Folder or prefix to search.</param>
    /// <returns>List of matching object paths.</returns>
    /// <example>
    /// <code>
    /// var keys = await storageProvider.ListAsync("users/");
    /// </code>
    /// </example>
    Task<List<string>> ListAsync(string folderPath);

    /// <summary>
    /// Lists object paths under a folder-like prefix as a single page with continuation support.
    /// </summary>
    /// <param name="folderPath">Folder or prefix to search.</param>
    /// <param name="pageSize">Maximum number of keys to return for this page.</param>
    /// <param name="continuationToken">Continuation token returned by a previous call, if any.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A page of keys and continuation information.</returns>
    /// <example>
    /// <code>
    /// var page = await storageProvider.ListPageAsync("users/", 100, null);
    /// </code>
    /// </example>
    Task<StorageListPage> ListPageAsync(
        string folderPath,
        int pageSize,
        string? continuationToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sanitizes a raw blob name so it can be safely used by the provider.
    /// </summary>
    /// <param name="rawName">Original blob or folder name.</param>
    /// <returns>Provider-safe normalized name.</returns>
    /// <example>
    /// <code>
    /// var safeName = storageProvider.SanitizeBlobName("My Entity");
    /// </code>
    /// </example>
    string SanitizeBlobName(string rawName);
}