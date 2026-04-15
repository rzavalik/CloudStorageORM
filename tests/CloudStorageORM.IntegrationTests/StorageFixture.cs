using Azure.Storage.Blobs;

namespace CloudStorageORM.IntegrationTests.Azure;

public class StorageFixture : IAsyncLifetime
{
    private const string DevelopmentStorageConnectionString = "UseDevelopmentStorage=true";
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(2)
    };

    public string ConnectionString { get; } = DevelopmentStorageConnectionString;
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

        var blobServiceClient = new BlobServiceClient(ConnectionString, CreateBlobClientOptions(ConnectionString));
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

    private static BlobClientOptions CreateBlobClientOptions(string connectionString)
    {
        return string.Equals(connectionString, DevelopmentStorageConnectionString, StringComparison.OrdinalIgnoreCase)
            // Keep emulator compatibility when SDK default service versions move forward.
            ? new BlobClientOptions(BlobClientOptions.ServiceVersion.V2021_12_02)
            : new BlobClientOptions();
    }
}