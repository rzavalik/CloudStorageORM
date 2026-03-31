using Microsoft.EntityFrameworkCore.Storage;

namespace CloudStorageORM.Infrastructure;

public class CloudStorageTransactionManager : IDbContextTransactionManager
{
    private readonly Lock _sync = new();
    private readonly List<Func<CancellationToken, Task>> _pendingOperations = [];
    private CloudStorageDbContextTransaction? _currentTransaction;

    public IDbContextTransaction BeginTransaction()
    {
        lock (_sync)
        {
            if (_currentTransaction is not null)
            {
                throw new InvalidOperationException("A transaction is already active for this context.");
            }

            _pendingOperations.Clear();
            _currentTransaction = new CloudStorageDbContextTransaction(this);
            return _currentTransaction;
        }
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(BeginTransaction());
    }

    public void CommitTransaction()
    {
        GetCurrentTransactionOrThrow().Commit();
    }

    public void RollbackTransaction()
    {
        GetCurrentTransactionOrThrow().Rollback();
    }

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        return GetCurrentTransactionOrThrow().CommitAsync(cancellationToken);
    }

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        return GetCurrentTransactionOrThrow().RollbackAsync(cancellationToken);
    }

    public void ResetState()
    {
        lock (_sync)
        {
            _pendingOperations.Clear();
            _currentTransaction = null;
        }
    }

    public Task ResetStateAsync(CancellationToken cancellationToken = default)
    {
        ResetState();
        return Task.CompletedTask;
    }

    public IDbContextTransaction? CurrentTransaction
    {
        get
        {
            lock (_sync)
            {
                return _currentTransaction;
            }
        }
    }

    internal bool HasActiveTransaction
    {
        get
        {
            lock (_sync)
            {
                return _currentTransaction is not null;
            }
        }
    }

    internal void EnqueueOperation(Func<CancellationToken, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (_sync)
        {
            if (_currentTransaction is null)
            {
                throw new InvalidOperationException("No active transaction is available to stage operations.");
            }

            _pendingOperations.Add(operation);
        }
    }

    internal async Task CommitTransactionInternalAsync(CloudStorageDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var operations = CaptureAndCloseTransaction(transaction);

        foreach (var operation in operations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await operation(cancellationToken);
        }
    }

    internal Task RollbackTransactionInternalAsync(CloudStorageDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CaptureAndCloseTransaction(transaction);
        return Task.CompletedTask;
    }

    private CloudStorageDbContextTransaction GetCurrentTransactionOrThrow()
    {
        lock (_sync)
        {
            return _currentTransaction
                   ?? throw new InvalidOperationException("No active transaction is available for this context.");
        }
    }

    private IReadOnlyList<Func<CancellationToken, Task>> CaptureAndCloseTransaction(
        CloudStorageDbContextTransaction transaction)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_currentTransaction, transaction))
            {
                throw new InvalidOperationException("Transaction is no longer active for this context.");
            }

            var operations = _pendingOperations.ToArray();
            _pendingOperations.Clear();
            _currentTransaction = null;
            return operations;
        }
    }
}

public sealed class CloudStorageDbContextTransaction(CloudStorageTransactionManager transactionManager)
    : IDbContextTransaction
{
    private readonly CloudStorageTransactionManager _transactionManager = transactionManager;
    private bool _completed;

    public Guid TransactionId { get; } = Guid.NewGuid();

    public void Commit()
    {
        CommitAsync().GetAwaiter().GetResult();
    }

    public void Rollback()
    {
        RollbackAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (_completed)
        {
            return;
        }

        Rollback();
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_completed)
        {
            return;
        }

        await _transactionManager.CommitTransactionInternalAsync(this, cancellationToken);
        _completed = true;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_completed)
        {
            return;
        }

        await _transactionManager.RollbackTransactionInternalAsync(this, cancellationToken);
        _completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_completed)
        {
            return;
        }

        await RollbackAsync();
    }
}
