# Transactions

CloudStorageORM provides transaction support through a durable transaction journal and replay mechanism. This guide
explains how transactions work and how to use them.

## Transaction basics

Transactions in CloudStorageORM are **context-scoped** and **durable**. Each transaction:

- Gets a unique `TransactionId` (`Guid`)
- Stages changes to a manifest on storage
- Can be committed, rolled back, or recovered after interruption

### Starting a transaction

```csharp
var transaction = await context.Database.BeginTransactionAsync();
```

### Committing a transaction

```csharp
try
{
    var user = new User { Name = "Alice", Email = "alice@example.com" };
    context.Add(user);
    await context.SaveChangesAsync();

    await transaction.CommitAsync();
    // Changes are now durably persisted
}
catch (Exception)
{
    await transaction.RollbackAsync();
    throw;
}
```

### Rolling back

```csharp
try
{
    // Make some changes
    context.Add(entity);
    await context.SaveChangesAsync();
}
catch
{
    await transaction.RollbackAsync();
    // Changes are discarded
}
```

## Transaction lifecycle

### Phase 1: Preparing

When you call `SaveChangesAsync()` inside an active transaction:

```
__cloudstorageorm/tx/<transactionId>/manifest.json
{
  "state": "Preparing",
  "operations": [
    { "type": "SaveAsync", "entity": "..." },
    { "type": "DeleteAsync", "entity": "..." }
  ]
}
```

### Phase 2: Committing

When you call `CommitAsync()`:

1. Manifest state changes to `Committed`
2. Operations are replayed in sequence with progress tracking
3. Manifest state changes to `Completed`

When optimistic concurrency is enabled (`UseObjectETagConcurrency(...)`), staged updates and deletes keep the
original `If-Match` ETag precondition in the durable manifest. During replay, commit uses those same conditional
provider calls.

If the object changed after staging but before commit replay, `CommitAsync()` throws `DbUpdateConcurrencyException`.
This matches the non-transactional `SaveChangesAsync()` conflict behavior.

### Phase 2.5: Crash-safe replay and idempotence

**Failure window**: Between marking a manifest as `Committed` and transitioning it to `Completed`, operations are being applied to storage. If the process interrupts during this window (network timeout, process crash, etc.), the transaction can be safely recovered.

**Operation-level progress tracking**: Each operation in the manifest has a sequence number. As operations are applied to storage, the manifest tracks how many operations have been successfully applied (`AppliedOperationCount`). This enables safe resumption after interruption.

**Resumable replay**: On recovery, when a `Committed` manifest is discovered:
- If `AppliedOperationCount < Operations.Count`, replay resumes from the last successfully applied operation (skipping already-applied operations)
- If `AppliedOperationCount == Operations.Count`, all operations have been applied, and the manifest transitions directly to `Completed`

**Idempotence guarantee**: Operations are designed to be safely re-applied:
- **Save operations** overwrite existing objects atomically, so re-saving an already-applied operation is safe
- **Delete operations** on missing objects are tolerated (idempotent)

This means re-running recovery on the same manifest is safe and produces no duplicate side effects.

### Phase 3: Recovery

On startup of a new transaction manager instance:

- **Completed manifests**: Are treated as finalized/completed state (no action needed)
- **Preparing manifests**: Are marked as `Aborted` (uncommitted work is discarded)
- **Committed manifests**: Are replayed to ensure durability
  - If partially applied (due to prior interruption), replay resumes from the last applied operation
  - If fully applied, manifest transitions directly to `Completed`

Recovery is deterministic and idempotent—running it multiple times produces the same result.

## Example: Transaction with retry

```csharp
var maxRetries = 3;
var retryCount = 0;

while (retryCount < maxRetries)
{
    try
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        var user = new User { Name = "Bob", Email = "bob@example.com" };
        context.Add(user);
        await context.SaveChangesAsync();

        await transaction.CommitAsync();
        break; // Success
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        retryCount++;
        if (retryCount >= maxRetries) throw;
    }
}
```

## Limitations

- Only **one active transaction** per `DbContext` instance
- Transactions are **not distributed** across contexts
- Provider-native temporary locking (for example, Azure leases) is **not yet implemented**

## Future enhancements

See [Roadmap](../ROADMAP.md) for planned improvements:

- v1.1.0: Provider-native locking (Azure leases, AWS conditional locks)
- v1.2.0: Enhanced AWS transaction reliability
- v1.4.0: Snapshot and point-in-time recovery

## See also

- [Getting started](getting-started.md)
- [Configuration](configuration.md)
- [Concurrency](concurrency.md)