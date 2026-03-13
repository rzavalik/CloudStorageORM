namespace CloudStorageORM.Tests.Infrastructure
{
    using Enums;
    using Options;
    using Providers;
    using Providers.Azure.StorageProviders;
    using Shouldly;

    public class ProviderFactoryTests
    {
        [Fact]
        public void GetStorageProvider_Azure_ReturnsAzureProvider()
        {
            var options = new CloudStorageOptions
            {
                Provider = CloudProvider.Azure,
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "provider-tests"
            };

            var provider = ProviderFactory.GetStorageProvider(options);
            provider.ShouldBeOfType<AzureBlobStorageProvider>();
        }

        [Theory]
        [InlineData(CloudProvider.Aws)]
        [InlineData(CloudProvider.Gcp)]
        public void GetStorageProvider_Unsupported_Throws(CloudProvider cloud)
        {
            var options = new CloudStorageOptions { Provider = cloud };
            var ex = Should.Throw<NotSupportedException>(() => ProviderFactory.GetStorageProvider(options));
            ex.Message.ShouldContain("not supported");
        }
    }
}