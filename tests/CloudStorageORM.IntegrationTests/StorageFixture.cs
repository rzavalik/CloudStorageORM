namespace CloudStorageORM.IntegrationTests.Azure
{
    using System.Threading.Tasks;
    using global::Azure.Storage.Blobs;

    public class StorageFixture : IAsyncLifetime
    {
        public string ConnectionString { get; }
        public string ContainerName { get; }

        public StorageFixture()
        {
            ConnectionString = "UseDevelopmentStorage=true";
            ContainerName = "test-container";
        }

        public async Task InitializeAsync()
        {
            var blobServiceClient = new BlobServiceClient(ConnectionString);

            var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

            await containerClient.CreateIfNotExistsAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
