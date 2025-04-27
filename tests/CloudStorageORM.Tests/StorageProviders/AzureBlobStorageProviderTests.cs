namespace CloudStorageORM.Tests.StorageProviders
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using CloudStorageORM.StorageProviders;
    using Shouldly;
    using Xunit;

    public class AzureBlobStorageProviderTests
    {
        // TODO: Mocks and Fakes here (later if needed)

        [Fact]
        public void Constructor_ShouldThrowArgumentException_WhenConnectionStringIsNullOrEmpty()
        {
            Should.Throw<ArgumentException>(() =>
            {
                var provider = new AzureBlobStorageProvider(null, "test-container");
            });

            Should.Throw<ArgumentException>(() =>
            {
                var provider = new AzureBlobStorageProvider(string.Empty, "test-container");
            });
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentException_WhenContainerNameIsNullOrEmpty()
        {
            Should.Throw<ArgumentException>(() =>
            {
                var provider = new AzureBlobStorageProvider("UseDevelopmentStorage=true", null);
            });

            Should.Throw<ArgumentException>(() =>
            {
                var provider = new AzureBlobStorageProvider("UseDevelopmentStorage=true", string.Empty);
            });
        }

        [Fact(Skip = "Integration test placeholder - requires real Azure Storage or Mock")]
        public async Task SaveAsync_ShouldSaveEntity()
        {
            // This would be a real integration test or a mock-based test
        }

        [Fact(Skip = "Integration test placeholder - requires real Azure Storage or Mock")]
        public async Task ReadAsync_ShouldReturnEntity()
        {
            // This would be a real integration test or a mock-based test
        }

        [Fact(Skip = "Integration test placeholder - requires real Azure Storage or Mock")]
        public async Task DeleteAsync_ShouldDeleteEntity()
        {
            // This would be a real integration test or a mock-based test
        }

        [Fact(Skip = "Integration test placeholder - requires real Azure Storage or Mock")]
        public async Task ListAsync_ShouldReturnListOfEntities()
        {
            // This would be a real integration test or a mock-based test
        }
    }
}
