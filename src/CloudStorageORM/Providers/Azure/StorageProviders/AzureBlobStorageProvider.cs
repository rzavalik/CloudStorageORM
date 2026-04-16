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

/// <summary>
/// Azure Blob Storage implementation of <see cref="IStorageProvider" />.
/// </summary>
public class AzureBlobStorageProvider : IStorageProvider
{
    private readonly BlobContainerClient _containerClient;
    private const string DevelopmentStorageConnectionString = "UseDevelopmentStorage=true";
    private static readonly JsonSerializerOptions ReadSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

    /// <summary>
    /// Creates a new Azure Blob Storage provider from CloudStorageORM options.
    /// </summary>
    /// <param name="options">Validated CloudStorageORM options containing the Azure connection string and container name.</param>
    /// <example>
    /// <code>
    /// var provider = new AzureBlobStorageProvider(options);
    /// </code>
    /// </example>
    public AzureBlobStorageProvider(CloudStorageOptions options)
    {
        _containerClient = OptionsContainerClientFactory(options);
        _containerClient.CreateIfNotExists();
    }

    /// <summary>
    /// Creates a new Azure Blob Storage provider using an existing container client.
    /// </summary>
    /// <param name="blobServiceClient">The container client to use for storage operations.</param>
    /// <example>
    /// <code>
    /// var provider = new AzureBlobStorageProvider(containerClient);
    /// </code>
    /// </example>
    public AzureBlobStorageProvider(BlobContainerClient blobServiceClient)
    {
        _containerClient = blobServiceClient;
        _containerClient.CreateIfNotExists();
    }

    /// <summary>
    /// Creates a new Azure Blob Storage provider from a connection string and container name.
    /// </summary>
    /// <param name="connectionString">Azure Storage connection string.</param>
    /// <param name="containerName">Azure container name.</param>
    /// <example>
    /// <code>
    /// var provider = new AzureBlobStorageProvider("UseDevelopmentStorage=true", "app-data");
    /// </code>
    /// </example>
    public AzureBlobStorageProvider(string connectionString, string containerName)
    {
        _containerClient = ConnectionContainerClientFactory(connectionString, containerName);
        _containerClient.CreateIfNotExists();
    }

    /// <inheritdoc />
    public CloudProvider CloudProvider => CloudProvider.Azure;

    /// <inheritdoc />
    public async Task SaveAsync<T>(string path, T entity)
    {
        await SaveAsync(path, entity, ifMatchETag: null);
    }

    /// <inheritdoc />
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
                if (!string.IsNullOrWhiteSpace(etag))
                {
                    return etag;
                }

                return await TryGetBlobETagAsync(blobClient);
            }

            var conditionalResponse = await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                Conditions = new BlobRequestConditions
                {
                    IfMatch = new ETag(ifMatchETag)
                }
            });

            var conditionalETag = conditionalResponse.Value?.ETag.ToString();
            if (!string.IsNullOrWhiteSpace(conditionalETag))
            {
                return conditionalETag;
            }

            return await TryGetBlobETagAsync(blobClient);
        }
        catch (RequestFailedException ex) when (ex.Status == 412)
        {
            throw new StoragePreconditionFailedException(path, ex);
        }
    }

    /// <inheritdoc />
    public async Task<T> ReadAsync<T>(string path)
    {
        var storageObject = await ReadWithMetadataAsync<T>(path);
        return storageObject.Value!;
    }

    /// <inheritdoc />
    public async Task<StorageObject<T>> ReadWithMetadataAsync<T>(string path)
    {
        var blobClient = _containerClient.GetBlobClient(path);
        if (!await blobClient.ExistsAsync())
        {
            return new StorageObject<T>(default, null, false);
        }

        var response = await blobClient.DownloadContentAsync();
        var etag = await TryGetBlobETagAsync(blobClient);
        var entity = JsonSerializer.Deserialize<T>(response.Value.Content.ToString(), ReadSerializerOptions);
        return new StorageObject<T>(entity, etag, true);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string path)
    {
        await DeleteAsync(path, ifMatchETag: null);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<List<string>> ListAsync(string folderPath)
    {
        var result = new List<string>();
        string? continuationToken = null;

        do
        {
            var page = await ListPageAsync(folderPath, pageSize: 1000, continuationToken, CancellationToken.None);
            result.AddRange(page.Keys);
            continuationToken = page.HasMore ? page.ContinuationToken : null;
        } while (!string.IsNullOrWhiteSpace(continuationToken));

        return result;
    }

    /// <inheritdoc />
    public async Task<StorageListPage> ListPageAsync(
        string folderPath,
        int pageSize,
        string? continuationToken,
        CancellationToken cancellationToken = default)
    {
        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than zero.");
        }

        await foreach (var page in _containerClient
                           .GetBlobsAsync(
                               BlobTraits.None,
                               BlobStates.None,
                               folderPath,
                               cancellationToken)
                           .AsPages(continuationToken, pageSizeHint: pageSize)
                           .WithCancellation(cancellationToken))
        {
            var keys = page.Values.Select(x => x.Name).ToList();
            var nextToken = string.IsNullOrWhiteSpace(page.ContinuationToken) ? null : page.ContinuationToken;
            return new StorageListPage(keys, nextToken, !string.IsNullOrWhiteSpace(nextToken));
        }

        return new StorageListPage([], null, false);
    }

    /// <inheritdoc />
    public async Task DeleteContainerAsync() => await _containerClient.DeleteIfExistsAsync();

    /// <inheritdoc />
    public async Task CreateContainerIfNotExistsAsync()
    {
        await _containerClient.CreateIfNotExistsAsync();
    }

    /// <inheritdoc />
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

    private static async Task<string?> TryGetBlobETagAsync(BlobClient blobClient)
    {
        var propertiesTask = blobClient.GetPropertiesAsync();
        if (propertiesTask is null)
        {
            return null;
        }

        var propertiesResponse = await propertiesTask;
        return propertiesResponse?.Value?.ETag.ToString();
    }
}