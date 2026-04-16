using CloudStorageORM.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

public class CloudStorageTransientExceptionClassifierTests
{
    [Fact]
    public void IsTransient_WithHttpRequestException_ReturnsTrue()
    {
        CloudStorageTransientExceptionClassifier.IsTransient(new HttpRequestException("network")).ShouldBeTrue();
    }

    [Fact]
    public void IsTransient_WithTimeoutException_ReturnsTrue()
    {
        CloudStorageTransientExceptionClassifier.IsTransient(new TimeoutException("timeout")).ShouldBeTrue();
    }

    [Fact]
    public void IsTransient_WithStoragePreconditionFailedException_ReturnsFalse()
    {
        CloudStorageTransientExceptionClassifier.IsTransient(new StoragePreconditionFailedException("users/1.json"))
            .ShouldBeFalse();
    }

    [Fact]
    public void IsTransient_WithDbUpdateConcurrencyException_ReturnsFalse()
    {
        CloudStorageTransientExceptionClassifier.IsTransient(new DbUpdateConcurrencyException("conflict"))
            .ShouldBeFalse();
    }

    [Fact]
    public void IsTransient_WithWrappedTransientException_ReturnsTrue()
    {
        var wrapped = new InvalidOperationException("wrapper", new HttpRequestException("network"));

        CloudStorageTransientExceptionClassifier.IsTransient(wrapped).ShouldBeTrue();
    }

    [Fact]
    public void IsTransient_WithNonTransientException_ReturnsFalse()
    {
        CloudStorageTransientExceptionClassifier.IsTransient(new InvalidOperationException("fatal")).ShouldBeFalse();
    }
}