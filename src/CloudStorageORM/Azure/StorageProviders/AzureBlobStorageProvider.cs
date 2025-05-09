namespace CloudStorageORM.Azure.StorageProviders
{
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using global::Azure.Storage.Blobs;

    public class AzureBlobStorageProvider : IStorageProvider
    {
        private readonly CloudStorageOptions _options;
        private readonly BlobContainerClient _containerClient;

        public AzureBlobStorageProvider(
           CloudStorageOptions options)
        {
            _options = options;
            _containerClient = new BlobContainerClient(
                options.ConnectionString,
                options.ContainerName);
            _containerClient.CreateIfNotExists();
        }

        public AzureBlobStorageProvider(
            CloudStorageOptions options,
            BlobContainerClient blobServiceClient)
        {
            _options = options;
            _containerClient = blobServiceClient;
            _containerClient.CreateIfNotExists();
        }

        public AzureBlobStorageProvider(
            string connectionString,
            string containerName)
        {
            _options = new CloudStorageOptions
            {
                ConnectionString = connectionString,
                ContainerName = containerName
            };
            _containerClient = new BlobContainerClient(connectionString, containerName);
            _containerClient.CreateIfNotExists();
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
            if (await blobClient.ExistsAsync())
            {
                var response = await blobClient.DownloadContentAsync();
                return JsonSerializer.Deserialize<T>(response.Value.Content.ToString())!;
            }

            return default!;
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

        public async Task DeleteContainerAsync()
        {
            await _containerClient.DeleteIfExistsAsync();
        }

        public async Task CreateContainerIfNotExistsAsync()
        {
            await _containerClient.CreateIfNotExistsAsync();
        }
    }
}
