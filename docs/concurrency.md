# Concurrency

CloudStorageORM supports **optimistic concurrency** using ETags (entity tags) provided by cloud object storage. This
guide explains how to enable and use concurrency control.

## Overview

Optimistic concurrency uses version tags (ETags) to detect concurrent modifications. When two clients update the same
entity simultaneously, the storage provider rejects the second update with a conflict error.

## Enabling ETag concurrency

### Option 1: Shadow ETag property

Use a hidden ETag property (recommended for simple cases):

```csharp
modelBuilder.Entity<User>().UseObjectETagConcurrency();
```

### Option 2: Mapped ETag property

Map ETag to a public property:

```csharp
public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? ETag { get; set; } // Optional property
}

modelBuilder.Entity<User>()
    .UseObjectETagConcurrency(e => e.ETag);
```

### Option 3: IETag interface

Implement the optional `IETag` interface for automatic access:

```csharp
public class User : IETag
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? ETag { get; set; } // Implements IETag
}

modelBuilder.Entity<User>().UseObjectETagConcurrency(e => e.ETag);
```

## How it works

### Reading with concurrency

When you query an entity with concurrency enabled:

```csharp
var user = await context.Users.FirstOrDefaultAsync(x => x.Id == "123");
// ETag is automatically materialized from storage metadata
```

### Updating with concurrency

When you update and save:

```csharp
user.Name = "Updated Name";
context.Update(user);

try
{
    await context.SaveChangesAsync();
    // Success: storage provider verified ETag matches original
}
catch (DbUpdateConcurrencyException ex)
{
    // Conflict: another client modified the entity
    // Handle conflict: merge, reload, or overwrite
}
```

### Delete with concurrency

Deletes also check the ETag:

```csharp
context.Remove(user);

try
{
    await context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    // Entity was modified/deleted by another client
}
```

### Concurrency inside transactions

When changes are staged inside `BeginTransactionAsync()` and later committed, CloudStorageORM preserves the original
ETag preconditions in staged operations.

- Transactional commit replay still sends conditional update/delete requests.
- Stale ETags during replay surface as `DbUpdateConcurrencyException` from `CommitAsync()`.
- Non-transactional and transactional concurrency behavior are aligned.

## Conflict handling patterns

### Pattern 1: Reload and retry

```csharp
try
{
    await context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    // Reload the current version
    var entry = ex.Entries.Single();
    var databaseValues = await entry.GetDatabaseValuesAsync();
    entry.OriginalValues.SetValues(databaseValues);

    // Reapply your changes or prompt user
    // Then retry
    await context.SaveChangesAsync();
}
```

### Pattern 2: Last-write-wins

```csharp
try
{
    await context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    // Ignore the conflict and force save
    foreach (var entry in context.ChangeTracker.Entries())
    {
        entry.OriginalValues.SetValues(entry.GetDatabaseValues());
    }
    await context.SaveChangesAsync();
}
```

### Pattern 3: User intervention

```csharp
catch (DbUpdateConcurrencyException ex)
{
    Console.WriteLine("Entity was modified by another user.");
    Console.WriteLine("Do you want to (O)verwrite or (R)eload?");

    var choice = Console.ReadLine();
    if (choice?.ToUpper() == "O")
    {
        // Force overwrite (reload values to bypass concurrency check)
        foreach (var entry in context.ChangeTracker.Entries())
        {
            entry.OriginalValues.SetValues(entry.GetDatabaseValues());
        }
        await context.SaveChangesAsync();
    }
    else
    {
        // Reload and discard local changes
        entry.Reload();
    }
}
```

## Provider-specific behavior

### Azure Blob Storage

- Uses blob `ETag` from metadata
- Sends `If-Match` header on update/delete
- Returns `412 Precondition Failed` on conflict

### AWS S3

- Uses object `ETag` from metadata
- Sends conditional headers on update/delete
- Returns `412 Precondition Failed` on conflict

## Best practices

1. **Always handle `DbUpdateConcurrencyException`** in multi-user scenarios
2. **Use shadow ETags** for simple cases; mapped properties when you need access to ETag
3. **Implement conflict resolution** appropriate to your domain (merge, reload, overwrite)
4. **Test concurrent scenarios** with [LocalStack](testing-with-localstack.md) or [Azurite](testing-with-azurite.md)

## Limitations and future work

- Provider-native temporary locking (Azure leases, AWS object locks) is **not yet implemented** (planned for v1.1.0+)
- Distributed transactions across multiple providers are **not supported**

## See also

- [Getting started](getting-started.md)
- [Transactions](transactions.md)
- [Configuration](configuration.md)
- [API reference](api-reference.md)