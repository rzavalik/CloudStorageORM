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
2. Operations are replayed in sequence
3. Manifest state changes to `Completed`

### Phase 3: Recovery

On startup of a new transaction manager instance:

- **Completed manifests**: Are finalized and removed
- **Preparing manifests**: Are marked as `Aborted` (uncommitted work is discarded)
- **Committed manifests**: Are replayed to ensure durability

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