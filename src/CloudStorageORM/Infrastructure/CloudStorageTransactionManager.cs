using System.Text.Json;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore.Storage;

namespace CloudStorageORM.Infrastructure;

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

    public CloudStorageTransactionManager()
    {
    }

    public CloudStorageTransactionManager(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

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

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRecoveryCompletedAsync(cancellationToken);
        return BeginTransaction();
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
            _activeManifest = null;
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

    internal async Task StageSaveOperationAsync(string path, object entity, CancellationToken cancellationToken)
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
                PayloadJson = payloadJson
            });
        }

        await PersistManifestAsync(manifest, cancellationToken);
    }

    internal async Task StageDeleteOperationAsync(string path, CancellationToken cancellationToken)
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
                Path = path
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
                    await ApplyDurableOperationsAsync(manifest, cancellationToken);
                    manifest.State = DurableTransactionState.Completed;
                    manifest.CompletedAtUtc = DateTimeOffset.UtcNow;
                    await PersistManifestAsync(manifest, cancellationToken);
                    break;

                case DurableTransactionState.Preparing:
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

        foreach (var operation in manifest.Operations.OrderBy(x => x.Sequence))
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (operation.Kind)
            {
                case DurableTransactionOperationKind.Save:
                    var payloadJson = string.IsNullOrWhiteSpace(operation.PayloadJson) ? "{}" : operation.PayloadJson;
                    var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
                    await _storageProvider.SaveAsync(operation.Path, payload);
                    break;

                case DurableTransactionOperationKind.Delete:
                    await _storageProvider.DeleteAsync(operation.Path);
                    break;
            }
        }
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
        public int Sequence { get; set; }
        public DurableTransactionOperationKind Kind { get; set; }
        public string Path { get; set; } = string.Empty;
        public string? PayloadJson { get; set; }
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

        public static DurableTransactionManifest Create(Guid transactionId)
        {
            return new DurableTransactionManifest
            {
                TransactionId = transactionId,
                State = DurableTransactionState.Preparing,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
        }
    }
}