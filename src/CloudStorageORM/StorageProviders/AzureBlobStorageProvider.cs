namespace CloudStorageORM.StorageProviders
{
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;

    public class AzureBlobStorageProvider : IStorageProvider
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobStorageProvider(CloudStorageOptions options)
        {
            var blobServiceClient = new BlobServiceClient(options.ConnectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(options.ContainerName);

            _containerClient.CreateIfNotExists();
        }

        public async Task SaveAsync<T>(string path, T entity)
        {
            var blobClient = _containerClient.GetBlobClient(path);
            var json = JsonSerializer.Serialize(entity);
            using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        public async Task<T> ReadAsync<T>(string path)
        {
            var blobClient = _containerClient.GetBlobClient(path);
            if (await blobClient.ExistsAsync())
            {
                var response = await blobClient.DownloadContentAsync();
                return JsonSerializer.Deserialize<T>(response.Value.Content.ToString());
            }
            return default;
        }

        public async Task DeleteAsync(string path)
        {
            var blobClient = _containerClient.GetBlobClient(path);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<List<string>> ListAsync(string folderPath)
        {
            var result = new List<string>();
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: folderPath))
            {
                result.Add(blobItem.Name);
            }
            return result;
        }
    }
}
