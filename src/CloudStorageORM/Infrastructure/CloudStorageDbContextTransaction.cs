using Microsoft.EntityFrameworkCore.Storage;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// Transaction wrapper used by <see cref="CloudStorageTransactionManager" />.
/// </summary>
public sealed class CloudStorageDbContextTransaction(CloudStorageTransactionManager transactionManager)
    : IDbContextTransaction
{
    private bool _completed;

    /// <inheritdoc />
    public Guid TransactionId { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public void Commit()
    {
        CommitAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void Rollback()
    {
        RollbackAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_completed)
        {
            return;
        }

        Rollback();
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_completed)
        {
            return;
        }

        try
        {
            await transactionManager.CommitTransactionInternalAsync(this, cancellationToken);
        }
        finally
        {
            _completed = true;
        }
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_completed)
        {
            return;
        }

        try
        {
            await transactionManager.RollbackTransactionInternalAsync(this, cancellationToken);
        }
        finally
        {
            _completed = true;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_completed)
        {
            return;
        }

        await RollbackAsync();
    }
}