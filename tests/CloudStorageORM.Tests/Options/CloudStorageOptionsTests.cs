using CloudStorageORM.Options;
using Shouldly;

namespace CloudStorageORM.Tests.Options;

public class CloudStorageOptionsTests
{
    [Fact]
    public void AzureAndAwsSpecificFields_AreAssignable()
    {
        var options = new CloudStorageOptions
        {
            Azure =
            {
                ConnectionString = "UseDevelopmentStorage=true"
            },
            Aws =
            {
                AccessKeyId = "test-access-key",
                SecretAccessKey = "test-secret-key",
                Region = "us-east-1",
                ServiceUrl = "http://localhost:4566",
                ForcePathStyle = true
            }
        };

        options.Azure.ConnectionString.ShouldBe("UseDevelopmentStorage=true");
        options.Aws.AccessKeyId.ShouldBe("test-access-key");
        options.Aws.SecretAccessKey.ShouldBe("test-secret-key");
        options.Aws.Region.ShouldBe("us-east-1");
        options.Aws.ServiceUrl.ShouldBe("http://localhost:4566");
        options.Aws.ForcePathStyle.ShouldBeTrue();
    }

    [Fact]
    public void ProviderSpecificOptions_AreInitialized()
    {
        var options = new CloudStorageOptions();

        options.Azure.ShouldNotBeNull();
        options.Aws.ShouldNotBeNull();
        options.Retry.ShouldNotBeNull();
        options.Observability.ShouldNotBeNull();
    }

    [Fact]
    public void RetryOptions_DefaultToExplicitOptInWithBoundedValues()
    {
        var options = new CloudStorageOptions();

        options.Retry.Enabled.ShouldBeFalse();
        options.Retry.MaxRetries.ShouldBe(3);
        options.Retry.BaseDelay.ShouldBe(TimeSpan.FromMilliseconds(100));
        options.Retry.MaxDelay.ShouldBe(TimeSpan.FromSeconds(2));
        options.Retry.JitterFactor.ShouldBe(0.2d);
    }

    [Fact]
    public void RetryOptions_AreAssignable()
    {
        var options = new CloudStorageOptions
        {
            Retry =
            {
                Enabled = true,
                MaxRetries = 5,
                BaseDelay = TimeSpan.FromMilliseconds(150),
                MaxDelay = TimeSpan.FromSeconds(1),
                JitterFactor = 0.4d
            }
        };

        options.Retry.Enabled.ShouldBeTrue();
        options.Retry.MaxRetries.ShouldBe(5);
        options.Retry.BaseDelay.ShouldBe(TimeSpan.FromMilliseconds(150));
        options.Retry.MaxDelay.ShouldBe(TimeSpan.FromSeconds(1));
        options.Retry.JitterFactor.ShouldBe(0.4d);
    }

    [Fact]
    public void ObservabilityOptions_DefaultToEnabled()
    {
        var options = new CloudStorageOptions();

        options.Observability.EnableLogging.ShouldBeTrue();
        options.Observability.EnableTracing.ShouldBeTrue();
        options.Observability.EnableDiagnostics.ShouldBeTrue();
    }

    [Fact]
    public void ObservabilityOptions_AreAssignable()
    {
        var options = new CloudStorageOptions
        {
            Observability =
            {
                EnableLogging = false,
                EnableTracing = false,
                EnableDiagnostics = false,
                ActivitySourceName = "CloudStorageORM.Custom",
                DiagnosticListenerName = "CloudStorageORM.Custom.Diagnostics"
            }
        };

        options.Observability.EnableLogging.ShouldBeFalse();
        options.Observability.EnableTracing.ShouldBeFalse();
        options.Observability.EnableDiagnostics.ShouldBeFalse();
        options.Observability.GetActivitySourceName().ShouldBe("CloudStorageORM.Custom");
        options.Observability.GetDiagnosticListenerName().ShouldBe("CloudStorageORM.Custom.Diagnostics");
    }
}