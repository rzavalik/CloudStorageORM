using CloudStorageORM.Infrastructure;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

public class CloudStorageTransactionManagerTests
{
    private readonly CloudStorageTransactionManager _sut = new();

    [Fact]
    public void BeginTransaction_ReturnsNoopTransaction()
    {
        var tx = _sut.BeginTransaction();
        tx.ShouldNotBeNull();
        tx.ShouldBeOfType<NoopDbContextTransaction>();
    }

    [Fact]
    public async Task BeginTransactionAsync_ReturnsNoopTransaction()
    {
        var tx = await _sut.BeginTransactionAsync();
        tx.ShouldNotBeNull();
        tx.ShouldBeOfType<NoopDbContextTransaction>();
    }

    [Fact]
    public void CommitTransaction_DoesNotThrow()
        => Should.NotThrow(() => _sut.CommitTransaction());

    [Fact]
    public void RollbackTransaction_DoesNotThrow()
        => Should.NotThrow(() => _sut.RollbackTransaction());

    [Fact]
    public void CurrentTransaction_IsNull()
        => _sut.CurrentTransaction.ShouldBeNull();

    [Fact]
    public async Task CommitTransactionAsync_ThrowsNotImplementedException()
        => await Should.ThrowAsync<NotImplementedException>(() => _sut.CommitTransactionAsync());

    [Fact]
    public async Task RollbackTransactionAsync_ThrowsNotImplementedException()
        => await Should.ThrowAsync<NotImplementedException>(() => _sut.RollbackTransactionAsync());

    [Fact]
    public void ResetState_ThrowsNotImplementedException()
        => Should.Throw<NotImplementedException>(() => _sut.ResetState());

    [Fact]
    public async Task ResetStateAsync_ThrowsNotImplementedException()
        => await Should.ThrowAsync<NotImplementedException>(() => _sut.ResetStateAsync());
}

public class NoopDbContextTransactionTests
{
    private readonly NoopDbContextTransaction _sut = new();

    [Fact]
    public void TransactionId_IsGuid()
        => _sut.TransactionId.ShouldNotBe(Guid.Empty);

    [Fact]
    public void Commit_DoesNotThrow()
        => Should.NotThrow(() => _sut.Commit());

    [Fact]
    public void Rollback_DoesNotThrow()
        => Should.NotThrow(() => _sut.Rollback());

    [Fact]
    public void Dispose_DoesNotThrow()
        => Should.NotThrow(() => _sut.Dispose());

    [Fact]
    public async Task CommitAsync_ThrowsNotImplementedException()
        => await Should.ThrowAsync<NotImplementedException>(() => _sut.CommitAsync());

    [Fact]
    public async Task RollbackAsync_ThrowsNotImplementedException()
        => await Should.ThrowAsync<NotImplementedException>(() => _sut.RollbackAsync());

    [Fact]
    public async Task DisposeAsync_ThrowsNotImplementedException()
    {
        await Should.ThrowAsync<NotImplementedException>(async () => { await _sut.DisposeAsync(); });
    }
}