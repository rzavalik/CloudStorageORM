using CloudStorageORM.Observability;
using Shouldly;

namespace CloudStorageORM.Tests.Observability;

public class CloudStorageOrmObservabilityOptionsTests
{
    [Fact]
    public void Defaults_AreEnabled_AndNamesFallbackToCloudStorageOrm()
    {
        var options = new CloudStorageOrmObservabilityOptions();

        options.EnableLogging.ShouldBeTrue();
        options.EnableTracing.ShouldBeTrue();
        options.EnableDiagnostics.ShouldBeTrue();
        options.GetActivitySourceName().ShouldBe("CloudStorageORM");
        options.GetDiagnosticListenerName().ShouldBe("CloudStorageORM");
    }

    [Fact]
    public void GetNameMethods_ReturnCustomNames_WhenProvided()
    {
        var options = new CloudStorageOrmObservabilityOptions
        {
            ActivitySourceName = "CloudStorageORM.Custom.Activity",
            DiagnosticListenerName = "CloudStorageORM.Custom.Diagnostics"
        };

        options.GetActivitySourceName().ShouldBe("CloudStorageORM.Custom.Activity");
        options.GetDiagnosticListenerName().ShouldBe("CloudStorageORM.Custom.Diagnostics");
    }
}