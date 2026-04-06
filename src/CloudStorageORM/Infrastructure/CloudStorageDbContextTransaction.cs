using Microsoft.EntityFrameworkCore.Storage;

namespace CloudStorageORM.Infrastructure;

public sealed class CloudStorageDbContextTransaction(CloudStorageTransactionManager transactionManager)
    : IDbContextTransaction
{
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

        await transactionManager.CommitTransactionInternalAsync(this, cancellationToken);
        _completed = true;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_completed)
        {
            return;
        }

        await transactionManager.RollbackTransactionInternalAsync(this, cancellationToken);
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