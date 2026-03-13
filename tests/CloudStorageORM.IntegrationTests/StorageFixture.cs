namespace CloudStorageORM.IntegrationTests.Azure
{
    using global::Azure.Storage.Blobs;

    public class StorageFixture : IAsyncLifetime
    {
        public string ConnectionString { get; } = "UseDevelopmentStorage=true";
        public string ContainerName { get; } = "test-container";

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
