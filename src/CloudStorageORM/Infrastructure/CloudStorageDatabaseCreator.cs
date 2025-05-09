namespace CloudStorageORM.Infrastructure
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore.Storage;

    public class CloudStorageDatabaseCreator : IDatabaseCreator
    {
        public bool CanConnect()
        {
            throw new NotImplementedException();
        }

        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Create() { }

        public Task CreateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Delete() { }

        public Task DeleteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public bool EnsureCreated()
        {
            throw new NotImplementedException();
        }

        public Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public bool EnsureDeleted()
        {
            throw new NotImplementedException();
        }

        public Task<bool> EnsureDeletedAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public bool Exists() => true;

        public Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public bool HasTables() => true;

        public Task<bool> HasTablesAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
