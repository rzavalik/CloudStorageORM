namespace CloudStorageORM.Infrastructure
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore.Storage;

    public class CloudStorageTransactionManager : IDbContextTransactionManager
    {
        public IDbContextTransaction BeginTransaction()
        {
            return new NoopDbContextTransaction();
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IDbContextTransaction>(new NoopDbContextTransaction());
        }

        public void CommitTransaction()
        {
        }

        public void RollbackTransaction()
        {
        }

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void ResetState()
        {
            throw new NotImplementedException();
        }

        public Task ResetStateAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IDbContextTransaction CurrentTransaction => null;
    }

    public class NoopDbContextTransaction : IDbContextTransaction
    {
        public Guid TransactionId => Guid.NewGuid();

        public void Commit() { }

        public void Rollback() { }

        public void Dispose() { }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
