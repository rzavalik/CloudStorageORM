using CloudStorageORM.Infrastructure;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

public class CloudStorageTransactionManagerTests
{
    private readonly CloudStorageTransactionManager _sut = new();

    [Fact]
    public void BeginTransaction_ReturnsCloudStorageTransaction()
    {
        var tx = _sut.BeginTransaction();
        tx.ShouldNotBeNull();
        tx.ShouldBeOfType<CloudStorageDbContextTransaction>();
        _sut.CurrentTransaction.ShouldBeSameAs(tx);
    }

    [Fact]
    public async Task BeginTransactionAsync_ReturnsCloudStorageTransaction()
    {
        var tx = await _sut.BeginTransactionAsync();
        tx.ShouldNotBeNull();
        tx.ShouldBeOfType<CloudStorageDbContextTransaction>();
        _sut.CurrentTransaction.ShouldBeSameAs(tx);
    }

    [Fact]
    public async Task CommitTransaction_CommitsAndClearsCurrentTransaction()
    {
        await _sut.BeginTransactionAsync();
        var committed = false;

        _sut.EnqueueOperation(_ =>
        {
            committed = true;
            return Task.CompletedTask;
        });

        await _sut.CommitTransactionAsync();

        committed.ShouldBeTrue();
        _sut.CurrentTransaction.ShouldBeNull();

        // Prevent state leakage if an assertion fails earlier.
        await _sut.ResetStateAsync();
    }

    [Fact]
    public void RollbackTransaction_DiscardsPendingOperations()
    {
        _sut.BeginTransaction();
        var committed = false;

        _sut.EnqueueOperation(_ =>
        {
            committed = true;
            return Task.CompletedTask;
        });

        _sut.RollbackTransaction();

        committed.ShouldBeFalse();
        _sut.CurrentTransaction.ShouldBeNull();
    }

    [Fact]
    public void BeginTransaction_WhenAlreadyActive_Throws()
    {
        _sut.BeginTransaction();

        var ex = Should.Throw<InvalidOperationException>(() => _sut.BeginTransaction());
        ex.Message.ShouldContain("already active");
    }

    [Fact]
    public async Task CommitTransactionAsync_CommitsAndClearsCurrentTransaction()
    {
        await _sut.BeginTransactionAsync();
        var committed = false;

        _sut.EnqueueOperation(_ =>
        {
            committed = true;
            return Task.CompletedTask;
        });

        await _sut.CommitTransactionAsync();

        committed.ShouldBeTrue();
        _sut.CurrentTransaction.ShouldBeNull();
    }

    [Fact]
    public async Task CommitTransactionAsync_ExecutesPendingOperationsInOrder()
    {
        await _sut.BeginTransactionAsync();
        var executed = new List<int>();

        _sut.EnqueueOperation(_ =>
        {
            executed.Add(1);
            return Task.CompletedTask;
        });
        _sut.EnqueueOperation(_ =>
        {
            executed.Add(2);
            return Task.CompletedTask;
        });

        await _sut.CommitTransactionAsync();

        executed.ShouldBe([1, 2]);
    }

    [Fact]
    public async Task RollbackTransactionAsync_DiscardsPendingOperations()
    {
        await _sut.BeginTransactionAsync();
        var committed = false;

        _sut.EnqueueOperation(_ =>
        {
            committed = true;
            return Task.CompletedTask;
        });

        await _sut.RollbackTransactionAsync();

        committed.ShouldBeFalse();
        _sut.CurrentTransaction.ShouldBeNull();
    }

    [Fact]
    public void ResetState_ClearsCurrentTransaction()
    {
        _sut.BeginTransaction();

        _sut.ResetState();

        _sut.CurrentTransaction.ShouldBeNull();
    }

    [Fact]
    public async Task ResetStateAsync_ClearsCurrentTransaction()
    {
        await _sut.BeginTransactionAsync();

        await _sut.ResetStateAsync();

        _sut.CurrentTransaction.ShouldBeNull();
    }
}

public class CloudStorageDbContextTransactionTests
{
    [Fact]
    public void Dispose_WithoutCommit_RollsBackPendingOperations()
    {
        var manager = new CloudStorageTransactionManager();
        var tx = manager.BeginTransaction();
        var committed = false;

        manager.EnqueueOperation(_ =>
        {
            committed = true;
            return Task.CompletedTask;
        });

        tx.Dispose();

        committed.ShouldBeFalse();
        manager.CurrentTransaction.ShouldBeNull();
    }

    [Fact]
    public void TransactionId_IsGuid()
    {
        var manager = new CloudStorageTransactionManager();
        var tx = manager.BeginTransaction();
        tx.TransactionId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task TransactionId_IsUniquePerTransaction()
    {
        var manager = new CloudStorageTransactionManager();

        var first = manager.BeginTransaction();
        var firstId = first.TransactionId;
        await first.RollbackAsync();

        var second = manager.BeginTransaction();
        var secondId = second.TransactionId;

        secondId.ShouldNotBe(firstId);
    }
}