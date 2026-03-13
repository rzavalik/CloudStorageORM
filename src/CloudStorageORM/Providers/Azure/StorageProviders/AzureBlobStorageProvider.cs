namespace CloudStorageORM.Providers.Azure.StorageProviders
{
    using System.Text;
    using System.Text.Json;
    using Enums;
    using global::Azure.Storage.Blobs;
    using Interfaces.StorageProviders;
    using Options;

    public class AzureBlobStorageProvider : IStorageProvider
    {
        private readonly BlobContainerClient _containerClient;

        private static Func<CloudStorageOptions, BlobContainerClient> OptionsContainerClientFactory { get; set; }
            = options => new BlobContainerClient(options.ConnectionString, options.ContainerName);

        internal static Func<string, string, BlobContainerClient> ConnectionContainerClientFactory { get; set; }
            = (connectionString, containerName) => new BlobContainerClient(connectionString, containerName);

        public AzureBlobStorageProvider(
           CloudStorageOptions options)
        {
            _containerClient = OptionsContainerClientFactory(options);
            _containerClient.CreateIfNotExists();
        }

        public AzureBlobStorageProvider(
            CloudStorageOptions options,
            BlobContainerClient blobServiceClient)
        {
            _containerClient = blobServiceClient;
            _containerClient.CreateIfNotExists();
        }

        public AzureBlobStorageProvider(
            string connectionString,
            string containerName)
        {
            var cloudStorageOptions = new CloudStorageOptions
            {
                ConnectionString = connectionString,
                ContainerName = containerName
            };
            _containerClient = ConnectionContainerClientFactory(connectionString, containerName);
            _containerClient.CreateIfNotExists();
        }

        public CloudProvider CloudProvider => CloudProvider.Azure;

        public async Task SaveAsync<T>(string path, T entity)
        {
            var blobClient = _containerClient.GetBlobClient(path);
            var json = JsonSerializer.Serialize(entity);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
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
    }
}
