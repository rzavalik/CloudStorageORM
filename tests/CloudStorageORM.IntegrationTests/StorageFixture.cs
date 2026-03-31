using Azure.Storage.Blobs;

namespace CloudStorageORM.IntegrationTests.Azure;

public class StorageFixture : IAsyncLifetime
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(2)
    };

    public string ConnectionString { get; } = "UseDevelopmentStorage=true";
    public string ContainerName { get; } = "test-container";
    public string? SkipReason { get; private set; }
    public bool IsAvailable => SkipReason is null;

    public async Task InitializeAsync()
    {
        if (!await IsAzuriteReachableAsync())
        {
            SkipReason = "Azurite is not reachable at http://127.0.0.1:10000.";
            return;
        }

        var blobServiceClient = new BlobServiceClient(ConnectionString);

        var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

        await containerClient.CreateIfNotExistsAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public void EnsureAvailableOrSkip()
    {
        IntegrationTestSkip.IfUnavailable(SkipReason);
    }

    private static async Task<bool> IsAzuriteReachableAsync()
    {
        try
        {
            using var response = await HttpClient.GetAsync("http://127.0.0.1:10000");
            return true;
        }
        catch
        {
            return false;
        }
    }
}