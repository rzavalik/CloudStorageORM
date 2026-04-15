using System.Diagnostics;
using CloudStorageORM.Observability;
using Shouldly;

namespace CloudStorageORM.Tests.Observability;

public class CloudStorageOrmActivitySourceTests
{
    [Fact]
    public void Instance_ReturnsSameSingleton()
    {
        var first = CloudStorageOrmActivitySource.Instance;
        var second = CloudStorageOrmActivitySource.Instance;

        ReferenceEquals(first, second).ShouldBeTrue();
        first.Name.ShouldBe("CloudStorageORM");
    }

    [Fact]
    public void StartActivity_CreatesActivity_WhenListenerIsRegistered()
    {
        using var listener = new ActivityListener();
        listener.ShouldListenTo = source => source.Name == "CloudStorageORM";
        listener.Sample = static (ref _) => ActivitySamplingResult.AllData;
        listener.SampleUsingParentId = static (ref _) => ActivitySamplingResult.AllData;

        ActivitySource.AddActivityListener(listener);

        using var activity = CloudStorageOrmActivitySource.StartActivity("SaveChanges");

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("SaveChanges");
    }

    [Fact]
    public void StartActivity_CanBeCalledWithoutAssertionsOnAmbientListeners()
    {
        _ = CloudStorageOrmActivitySource.StartActivity("Query");
    }
}