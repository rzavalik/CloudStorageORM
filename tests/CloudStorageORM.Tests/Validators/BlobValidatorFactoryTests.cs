using CloudStorageORM.Enums;
using CloudStorageORM.Interfaces.Validators;
using CloudStorageORM.Providers.Aws.Validators;
using CloudStorageORM.Providers.Azure.Validators;
using CloudStorageORM.Validators;
using Shouldly;

namespace CloudStorageORM.Tests.Validators;

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

    [Fact]
    public void Create_WithAwsProvider_ShouldReturnAwsBlobValidator()
    {
        var validator = BlobValidatorFactory.Create(CloudProvider.Aws);

        validator.ShouldNotBeNull();
        validator.ShouldBeOfType<AwsBlobValidator>();
        validator.ShouldBeAssignableTo<IBlobValidator>();
        validator.GetType().Name.ShouldBe("AwsBlobValidator");
    }

    [Theory]
    [InlineData(CloudProvider.Gcp)]
    public void Create_WithUnsupportedProvider_ShouldThrowNotSupportedException(CloudProvider provider)
    {
        var ex = Should.Throw<NotSupportedException>(() => BlobValidatorFactory.Create(provider));

        ex.Message.ShouldContain(provider.ToString());
    }
}