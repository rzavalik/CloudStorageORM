using System.Text.Json;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// Transaction manager that stages object-storage operations and optionally persists a durable journal.
/// </summary>
public class CloudStorageTransactionManager : IDbContextTransactionManager
{
    private const string TransactionPrefix = "__cloudstorageorm/tx";

    private readonly IStorageProvider? _storageProvider;
    private readonly Lock _sync = new();
    private readonly List<Func<CancellationToken, Task>> _pendingOperations = [];
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private CloudStorageDbContextTransaction? _currentTransaction;
    private DurableTransactionManifest? _activeManifest;
    private bool _recoveryCompleted;

    /// <summary>
    /// Creates a transaction manager without durable journal support.
    /// </summary>
    public CloudStorageTransactionManager()
    {
    }

    /// <summary>
    /// Creates a transaction manager with durable journal support backed by object storage.
    /// </summary>
    /// <param name="storageProvider">Storage provider used to persist transaction manifests.</param>
    public CloudStorageTransactionManager(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    /// <inheritdoc />
    public IDbContextTransaction BeginTransaction()
    {
        EnsureRecoveryCompletedAsync(CancellationToken.None).GetAwaiter().GetResult();

        lock (_sync)
        {
            if (_currentTransaction is not null)
            {
                throw new InvalidOperationException("A transaction is already active for this context.");
            }

            _pendingOperations.Clear();
            _currentTransaction = new CloudStorageDbContextTransaction(this);
            _activeManifest = DurableTransactionManifest.Create(_currentTransaction.TransactionId);

            return _currentTransaction;
        }
    }

    /// <inheritdoc />
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRecoveryCompletedAsync(cancellationToken);
        return BeginTransaction();
    }

    /// <inheritdoc />
    public void CommitTransaction()
    {
        GetCurrentTransactionOrThrow().Commit();
    }

    /// <inheritdoc />
    public void RollbackTransaction()
    {
        GetCurrentTransactionOrThrow().Rollback();
    }

    /// <inheritdoc />
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        return GetCurrentTransactionOrThrow().CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        return GetCurrentTransactionOrThrow().RollbackAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void ResetState()
    {
        lock (_sync)
        {
            _pendingOperations.Clear();
            _activeManifest = null;
            _currentTransaction = null;
        }
    }

    /// <inheritdoc />
    public Task ResetStateAsync(CancellationToken cancellationToken = default)
    {
        ResetState();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
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

    internal bool IsDurableJournalEnabled => _storageProvider is not null;

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

    internal async Task StageSaveOperationAsync(
        string path,
        object entity,
        string? ifMatchETag,
        CancellationToken cancellationToken)
    {
        var (transaction, manifest) = GetActiveTransactionState();

        if (_storageProvider is null)
        {
            EnqueueOperation(_ => throw new InvalidOperationException(
                "Durable transaction staging requires a configured storage provider."));
            return;
        }

        var payloadJson = JsonSerializer.Serialize(entity, _jsonOptions);

        lock (_sync)
        {
            EnsureTransactionStillActive(transaction);
            manifest.Operations.Add(new DurableTransactionOperation
            {
                Sequence = manifest.Operations.Count,
                Kind = DurableTransactionOperationKind.Save,
                Path = path,
                PayloadJson = payloadJson,
                IfMatchETag = ifMatchETag
            });
        }

        await PersistManifestAsync(manifest, cancellationToken);
    }

    internal async Task StageDeleteOperationAsync(string path, string? ifMatchETag, CancellationToken cancellationToken)
    {
        var (transaction, manifest) = GetActiveTransactionState();

        if (_storageProvider is null)
        {
            EnqueueOperation(_ => throw new InvalidOperationException(
                "Durable transaction staging requires a configured storage provider."));
            return;
        }

        lock (_sync)
        {
            EnsureTransactionStillActive(transaction);
            manifest.Operations.Add(new DurableTransactionOperation
            {
                Sequence = manifest.Operations.Count,
                Kind = DurableTransactionOperationKind.Delete,
                Path = path,
                IfMatchETag = ifMatchETag
            });
        }

        await PersistManifestAsync(manifest, cancellationToken);
    }

    internal async Task CommitTransactionInternalAsync(CloudStorageDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var state = CaptureAndCloseTransaction(transaction);

        if (_storageProvider is not null && state.Manifest is not null)
        {
            state.Manifest.State = DurableTransactionState.Committed;
            state.Manifest.CommittedAtUtc = DateTimeOffset.UtcNow;
            await PersistManifestAsync(state.Manifest, cancellationToken);

            await ApplyDurableOperationsAsync(state.Manifest, cancellationToken);

            state.Manifest.State = DurableTransactionState.Completed;
            state.Manifest.CompletedAtUtc = DateTimeOffset.UtcNow;
            await PersistManifestAsync(state.Manifest, cancellationToken);
        }

        foreach (var operation in state.PendingOperations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await operation(cancellationToken);
        }
    }

    internal Task RollbackTransactionInternalAsync(CloudStorageDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = CaptureAndCloseTransaction(transaction);

        if (_storageProvider is null || state.Manifest is null)
        {
            return Task.CompletedTask;
        }

        state.Manifest.State = DurableTransactionState.Aborted;
        state.Manifest.CompletedAtUtc = DateTimeOffset.UtcNow;
        return PersistManifestAsync(state.Manifest, cancellationToken);

    }

    private CloudStorageDbContextTransaction GetCurrentTransactionOrThrow()
    {
        lock (_sync)
        {
            return _currentTransaction
                   ?? throw new InvalidOperationException("No active transaction is available for this context.");
        }
    }

    private CapturedTransactionState CaptureAndCloseTransaction(
        CloudStorageDbContextTransaction transaction)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_currentTransaction, transaction))
            {
                throw new InvalidOperationException("Transaction is no longer active for this context.");
            }

            var operations = _pendingOperations.ToArray();
            var manifest = _activeManifest;
            _pendingOperations.Clear();
            _activeManifest = null;
            _currentTransaction = null;
            return new CapturedTransactionState(operations, manifest);
        }
    }

     private async Task EnsureRecoveryCompletedAsync(CancellationToken cancellationToken)
     {
         if (_storageProvider is null || _recoveryCompleted)
         {
             return;
         }

         var manifestPaths = await _storageProvider.ListAsync(TransactionPrefix);
         var manifests = manifestPaths.Where(path => path.EndsWith("/manifest.json", StringComparison.OrdinalIgnoreCase));

         foreach (var manifestPath in manifests)
         {
             cancellationToken.ThrowIfCancellationRequested();
             var manifest = await _storageProvider.ReadAsync<DurableTransactionManifest>(manifestPath);
             if (manifest.TransactionId == Guid.Empty)
             {
                 continue;
             }

             switch (manifest.State)
             {
                 case DurableTransactionState.Committed:
                     // Resume replay from where it left off, skipping already-applied operations.
                     // If all operations are applied (AppliedOperationCount == Operations.Count),
                     // this becomes a no-op and transitions directly to Completed.
                     await ApplyDurableOperationsAsync(manifest, cancellationToken);
                     manifest.State = DurableTransactionState.Completed;
                     manifest.CompletedAtUtc = DateTimeOffset.UtcNow;
                     await PersistManifestAsync(manifest, cancellationToken);
                     break;

                 case DurableTransactionState.Preparing:
                     // Uncommitted preparing transactions are safely aborted without losing data.
                     manifest.State = DurableTransactionState.Aborted;
                     manifest.CompletedAtUtc = DateTimeOffset.UtcNow;
                     await PersistManifestAsync(manifest, cancellationToken);
                     break;
             }
         }

         _recoveryCompleted = true;
     }

    private async Task PersistManifestAsync(DurableTransactionManifest manifest, CancellationToken cancellationToken)
    {
        if (_storageProvider is null)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await _storageProvider.SaveAsync(GetManifestPath(manifest.TransactionId), manifest);
    }

     private async Task ApplyDurableOperationsAsync(DurableTransactionManifest manifest, CancellationToken cancellationToken)
     {
         if (_storageProvider is null)
         {
             return;
         }

         var startIndex = manifest.AppliedOperationCount;
         var operations = manifest.Operations.OrderBy(x => x.Sequence).ToList();

         for (var i = startIndex; i < operations.Count; i++)
         {
             cancellationToken.ThrowIfCancellationRequested();

             var operation = operations[i];
             switch (operation.Kind)
             {
                 case DurableTransactionOperationKind.Save:
                     var payloadJson = string.IsNullOrWhiteSpace(operation.PayloadJson) ? "{}" : operation.PayloadJson;
                     var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
                     await SaveDurableOperationAsync(operation, payload);
                     break;

                 case DurableTransactionOperationKind.Delete:
                     await DeleteDurableOperationAsync(operation);
                     break;
             }

             // Update progress marker after each operation is applied successfully.
             manifest.AppliedOperationCount = i + 1;
             await PersistManifestAsync(manifest, cancellationToken);
         }
     }

    private async Task SaveDurableOperationAsync(DurableTransactionOperation operation, JsonElement payload)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(operation.IfMatchETag))
            {
                await _storageProvider!.SaveAsync(operation.Path, payload);
                return;
            }

            await _storageProvider!.SaveAsync(operation.Path, payload, operation.IfMatchETag);
        }
        catch (StoragePreconditionFailedException ex)
        {
            throw CreateConcurrencyException(operation.Path, ex);
        }
    }

    private async Task DeleteDurableOperationAsync(DurableTransactionOperation operation)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(operation.IfMatchETag))
            {
                await _storageProvider!.DeleteAsync(operation.Path);
                return;
            }

            await _storageProvider!.DeleteAsync(operation.Path, operation.IfMatchETag);
        }
        catch (StoragePreconditionFailedException ex)
        {
            throw CreateConcurrencyException(operation.Path, ex);
        }
    }

    private static DbUpdateConcurrencyException CreateConcurrencyException(string path, Exception innerException)
    {
        return new DbUpdateConcurrencyException(
            $"The operation expected the object ETag to match for '{path}', but it was updated by another process.",
            innerException);
    }

    private static string GetManifestPath(Guid transactionId)
        => $"{TransactionPrefix}/{transactionId:D}/manifest.json";

    private (CloudStorageDbContextTransaction Transaction, DurableTransactionManifest Manifest) GetActiveTransactionState()
    {
        lock (_sync)
        {
            var transaction = _currentTransaction
                              ?? throw new InvalidOperationException(
                                  "No active transaction is available to stage operations.");
            var manifest = _activeManifest
                           ?? throw new InvalidOperationException(
                               "No durable transaction manifest is available for the active transaction.");
            return (transaction, manifest);
        }
    }

    private void EnsureTransactionStillActive(CloudStorageDbContextTransaction transaction)
    {
        if (!ReferenceEquals(_currentTransaction, transaction))
        {
            throw new InvalidOperationException("Transaction is no longer active for this context.");
        }
    }

    private sealed record CapturedTransactionState(
        IReadOnlyList<Func<CancellationToken, Task>> PendingOperations,
        DurableTransactionManifest? Manifest);

    private enum DurableTransactionState
    {
        Preparing,
        Committed,
        Completed,
        Aborted
    }

    private enum DurableTransactionOperationKind
    {
        Save,
        Delete
    }

    private sealed class DurableTransactionOperation
    {
        public int Sequence { get; init; }
        public DurableTransactionOperationKind Kind { get; init; }
        public string Path { get; init; } = string.Empty;
        public string? PayloadJson { get; init; }
        public string? IfMatchETag { get; init; }
    }

     private sealed class DurableTransactionManifest
     {
         // ReSharper disable once MemberCanBePrivate.Local
         // ReSharper disable once PropertyCanBeMadeInitOnly.Local
         public Guid TransactionId { get; set; }
         public DurableTransactionState State { get; set; }
         // ReSharper disable once UnusedAutoPropertyAccessor.Local
         public DateTimeOffset CreatedAtUtc { get; set; }
         // ReSharper disable once UnusedAutoPropertyAccessor.Local
         public DateTimeOffset? CommittedAtUtc { get; set; }
         // ReSharper disable once UnusedAutoPropertyAccessor.Local
         public DateTimeOffset? CompletedAtUtc { get; set; }
         // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
         public List<DurableTransactionOperation> Operations { get; set; } = [];
         /// <summary>
         /// Tracks the number of operations that have been successfully applied to storage.
         /// Used to resume replay after interruption without re-applying operations.
         /// Value is 0 for new transactions, incremented as operations are applied during commit/recovery.
         /// When AppliedOperationCount equals Operations.Count, all operations have been applied.
         /// </summary>
         public int AppliedOperationCount { get; set; }

         /// <summary>
         /// Creates a new manifest for a transaction in the preparing state.
         /// </summary>
         /// <param name="transactionId">Unique transaction identifier.</param>
         /// <returns>A new durable transaction manifest.</returns>
         public static DurableTransactionManifest Create(Guid transactionId)
         {
             return new DurableTransactionManifest
             {
                 TransactionId = transactionId,
                 State = DurableTransactionState.Preparing,
                 CreatedAtUtc = DateTimeOffset.UtcNow,
                 AppliedOperationCount = 0
             };
         }
     }
}