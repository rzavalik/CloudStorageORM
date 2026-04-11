using Microsoft.EntityFrameworkCore.Storage;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// Lightweight database-creator implementation for the object-storage provider.
/// </summary>
public class CloudStorageDatabaseCreator : IDatabaseCreator
{
    /// <summary>
    /// Determines whether the provider can connect to its backing store.
    /// </summary>
    /// <returns><see langword="true" /> when a connection can be established.</returns>
    public bool CanConnect()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Determines whether the provider can connect to its backing store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when a connection can be established.</returns>
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Creates provider backing resources when applicable.
    /// </summary>
    public void Create()
    {
    }

    /// <summary>
    /// Creates provider backing resources when applicable.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task CreateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Deletes provider backing resources when applicable.
    /// </summary>
    public void Delete()
    {
    }

    /// <summary>
    /// Deletes provider backing resources when applicable.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task DeleteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Ensures backing resources exist.
    /// </summary>
    /// <returns><see langword="true" /> when resources were created during this call.</returns>
    public bool EnsureCreated()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Ensures backing resources exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when resources were created during this call.</returns>
    public Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Ensures backing resources are deleted.
    /// </summary>
    /// <returns><see langword="true" /> when resources were deleted during this call.</returns>
    public bool EnsureDeleted()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Ensures backing resources are deleted.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when resources were deleted during this call.</returns>
    public Task<bool> EnsureDeletedAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Checks whether backing resources currently exist.
    /// </summary>
    /// <returns><see langword="true" /> when resources exist.</returns>
    public bool Exists() => true;

    /// <summary>
    /// Checks whether backing resources currently exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when resources exist.</returns>
    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    /// <summary>
    /// Checks whether backing resources contain logical tables/collections.
    /// </summary>
    /// <returns><see langword="true" /> when logical tables are available.</returns>
    public bool HasTables() => true;

    /// <summary>
    /// Checks whether backing resources contain logical tables/collections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when logical tables are available.</returns>
    public Task<bool> HasTablesAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
}