using System.Text;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CloudStorageORM.Abstractions;
using CloudStorageORM.Enums;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Options;

namespace CloudStorageORM.Providers.Azure.StorageProviders;

public class AzureBlobStorageProvider : IStorageProvider
{
    private readonly BlobContainerClient _containerClient;
    private const string DevelopmentStorageConnectionString = "UseDevelopmentStorage=true";

    private static Func<CloudStorageOptions, BlobContainerClient> OptionsContainerClientFactory { get; set; }
        = options => new BlobContainerClient(
            options.Azure.ConnectionString,
            options.ContainerName,
            CreateBlobClientOptions(options.Azure.ConnectionString));

    internal static Func<string, string, BlobContainerClient> ConnectionContainerClientFactory { get; set; }
        = (connectionString, containerName) => new BlobContainerClient(
            connectionString,
            containerName,
            CreateBlobClientOptions(connectionString));

    public AzureBlobStorageProvider(
        CloudStorageOptions options)
    {
        _containerClient = OptionsContainerClientFactory(options);
        _containerClient.CreateIfNotExists();
    }

    public AzureBlobStorageProvider(BlobContainerClient blobServiceClient)
    {
        _containerClient = blobServiceClient;
        _containerClient.CreateIfNotExists();
    }

    public AzureBlobStorageProvider(
        string connectionString,
        string containerName)
    {
        _containerClient = ConnectionContainerClientFactory(connectionString, containerName);
        _containerClient.CreateIfNotExists();
    }

    public CloudProvider CloudProvider => CloudProvider.Azure;

    public async Task SaveAsync<T>(string path, T entity)
    {
        await SaveAsync(path, entity, ifMatchETag: null);
    }

    public async Task<string?> SaveAsync<T>(string path, T entity, string? ifMatchETag)
    {
        var blobClient = _containerClient.GetBlobClient(path);
        var json = JsonSerializer.Serialize(entity);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        try
        {
            if (string.IsNullOrWhiteSpace(ifMatchETag))
            {
                var response = await blobClient.UploadAsync(stream, overwrite: true);
                var etag = response.Value?.ETag.ToString();
                return !string.IsNullOrWhiteSpace(etag)
                    ? etag
                    : (await blobClient.GetPropertiesAsync()).Value.ETag.ToString();
            }

            var conditionalResponse = await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                Conditions = new BlobRequestConditions
                {
                    IfMatch = new ETag(ifMatchETag)
                }
            });

            var conditionalETag = conditionalResponse.Value?.ETag.ToString();
            return !string.IsNullOrWhiteSpace(conditionalETag)
                ? conditionalETag
                : (await blobClient.GetPropertiesAsync()).Value.ETag.ToString();
        }
        catch (RequestFailedException ex) when (ex.Status == 412)
        {
            throw new StoragePreconditionFailedException(path, ex);
        }
    }

    public async Task<T> ReadAsync<T>(string path)
    {
        var storageObject = await ReadWithMetadataAsync<T>(path);
        return storageObject.Value!;
    }

    public async Task<StorageObject<T>> ReadWithMetadataAsync<T>(string path)
    {
        var blobClient = _containerClient.GetBlobClient(path);
        if (!await blobClient.ExistsAsync())
        {
            return new StorageObject<T>(default, null, false);
        }

        var response = await blobClient.DownloadContentAsync();
        var properties = await blobClient.GetPropertiesAsync();
        var entity = JsonSerializer.Deserialize<T>(response.Value.Content.ToString());
        return new StorageObject<T>(entity, properties.Value.ETag.ToString(), true);
    }

    public async Task DeleteAsync(string path)
    {
        await DeleteAsync(path, ifMatchETag: null);
    }

    public async Task DeleteAsync(string path, string? ifMatchETag)
    {
        var blobClient = _containerClient.GetBlobClient(path);

        try
        {
            if (string.IsNullOrWhiteSpace(ifMatchETag))
            {
                await blobClient.DeleteIfExistsAsync();
                return;
            }

            await blobClient.DeleteIfExistsAsync(
                DeleteSnapshotsOption.None,
                new BlobRequestConditions
                {
                    IfMatch = new ETag(ifMatchETag)
                });
        }
        catch (RequestFailedException ex) when (ex.Status == 412)
        {
            throw new StoragePreconditionFailedException(path, ex);
        }
    }

    public async Task<List<string>> ListAsync(string folderPath)
    {
        var result = new List<string>();
        await foreach (var blobItem in _containerClient.GetBlobsAsync(
                           BlobTraits.None,
                           BlobStates.None,
                           folderPath,
                           CancellationToken.None))
        {
            result.Add(blobItem.Name);
        }

        return result;
    }

    public async Task DeleteContainerAsync() => await _containerClient.DeleteIfExistsAsync();

    public async Task CreateContainerIfNotExistsAsync()
    {
        await _containerClient.CreateIfNotExistsAsync();
    }

    public string SanitizeBlobName(string rawName)
    {
        var invalidChars = new[] { '\\', '/', '?', '#', '[', ']', ' ', '+', '`', '"' };
        var sanitizedName = new StringBuilder(rawName.Length);

        foreach (var c in rawName)
        {
            sanitizedName.Append(invalidChars.Contains(c) ? '_' : c);
        }

        return sanitizedName.ToString().ToLowerInvariant();
    }

    private static BlobClientOptions CreateBlobClientOptions(string connectionString)
    {
        return string.Equals(connectionString, DevelopmentStorageConnectionString, StringComparison.OrdinalIgnoreCase)
            // Keep emulator compatibility when SDK default service versions move forward.
            ? new BlobClientOptions(BlobClientOptions.ServiceVersion.V2021_12_02)
            : new BlobClientOptions();
    }
}