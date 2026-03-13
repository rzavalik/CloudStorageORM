namespace CloudStorageORM.Tests.Validators
{
    using CloudStorageORM.Validators;
    using Enums;
    using Interfaces.Validators;
    using Providers.Azure.Validators;
    using Shouldly;

    public class BlobValidatorFactoryTests
    {
        [Fact]
        public void Create_WithAzureProvider_ShouldReturnAzureBlobValidator()
        {
            var validator = BlobValidatorFactory.Create(CloudProvider.Azure);

            validator.ShouldNotBeNull();
            validator.ShouldBeOfType<AzureBlobValidator>();
            validator.ShouldBeAssignableTo<IBlobValidator>();
            validator.GetType().Name.ShouldBe("AzureBlobValidator");
        }

        [Theory]
        [InlineData(CloudProvider.Aws)]
        [InlineData(CloudProvider.Gcp)]
        public void Create_WithUnsupportedProvider_ShouldThrowNotSupportedException(CloudProvider provider)
        {
            var ex = Should.Throw<NotSupportedException>(() => BlobValidatorFactory.Create(provider));

            ex.Message.ShouldContain(provider.ToString());
        }
    }
}
