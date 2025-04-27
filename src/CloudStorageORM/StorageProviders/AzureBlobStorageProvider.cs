namespace CloudStorageORM.StorageProviders
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using CloudStorageORM.Abstractions;

    public class AzureBlobStorageProvider : IStorageProvider
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobStorageProvider(string connectionString, string containerName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must not be null or empty.", nameof(connectionString));

            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name must not be null or empty.", nameof(containerName));

            var blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        }

        public async Task SaveAsync<T>(string path, T entity)
        {
            var blobClient = _containerClient.GetBlobClient(path);
            var json = JsonSerializer.Serialize(entity);
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        public async Task<T> ReadAsync<T>(string path)
        {
            var blobClient = _containerClient.GetBlobClient(path);
            if (!await blobClient.ExistsAsync())
                return default;

            var downloadInfo = await blobClient.DownloadAsync();
            using var reader = new StreamReader(downloadInfo.Value.Content);
            var content = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<T>(content);
        }

        public async Task DeleteAsync(string path)
        {
            var blobClient = _containerClient.GetBlobClient(path);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<List<string>> ListAsync(string folderPath)
        {
            var results = new List<string>();
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: folderPath))
            {
                results.Add(blobItem.Name);
            }
            return results;
        }
    }
}
