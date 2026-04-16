# Troubleshooting

Common issues and solutions when using CloudStorageORM.

## Connection issues

### Azure: "UseDevelopmentStorage=true" not working

**Error**: `System.InvalidOperationException: No valid combination of account information found.`

**Cause**: Azurite is not running.

**Solution**:

```bash
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  mcr.microsoft.com/azure-storage/azurite:latest \
  azurite --blobHost 0.0.0.0 --skipApiVersionCheck
```

### AWS: Connection timeout

**Error**: `EndpointConnectionException: Unable to connect to the endpoint...`

**Cause**: LocalStack is not running or service URL is incorrect.

**Solution**:

```bash
docker run -d -p 4566:4566 \
  -e SERVICES=s3 \
  localstack/localstack:3

# Verify connection
aws s3 ls --endpoint-url=http://127.0.0.1:4566
```

### Repeated transient network failures

**Symptoms**: intermittent `HttpRequestException`, timeout, or service unavailable responses during save/query
operations.

**Cause**: temporary network/provider instability and retry policy disabled or under-sized.

**Solution**:

1. Enable retries explicitly under `CloudStorageOptions.Retry`.
2. Start with conservative values (`MaxRetries = 3`, `BaseDelay = 100ms`, `MaxDelay = 2s`, `JitterFactor = 0.2`).
3. Increase `MaxRetries` or delay budget only when failures are recoverable and latency budget allows it.
4. Keep in mind non-transient failures (for example stale ETag concurrency conflicts) are not retried.

```csharp
storage.Retry.Enabled = true;
storage.Retry.MaxRetries = 3;
storage.Retry.BaseDelay = TimeSpan.FromMilliseconds(100);
storage.Retry.MaxDelay = TimeSpan.FromSeconds(2);
storage.Retry.JitterFactor = 0.2;
```

### Invalid credentials

**Error**: `UnauthorizedAccessException` or `AccessDeniedException`

**Solution**:

1. Verify credentials are correct
2. For Azure: check connection string format
3. For AWS: ensure IAM permissions include s3:*, and try with test/test credentials in local environments
4. Check that the container/bucket exists

## Query issues

### "LINQ query evaluation failed"

**Cause**: CloudStorageORM cannot evaluate a complex LINQ expression directly.

**Solution**:

1. Move complex logic to post-materialization filtering:
   ```csharp
   // ❌ Won't work
   var users = await context.Users
       .Where(u => u.Email.Contains("@example.com"))
       .ToListAsync();

   // ✅ Use in-memory filtering
   var users = await context.Users.ToListAsync();
   var filtered = users
       .Where(u => u.Email.Contains("@example.com"))
       .ToList();
   ```

### No results from range queries

**Cause**: String comparison for IDs may not behave as expected.

**Solution**: Ensure IDs are comparable. If using guids/strings, consider:

```csharp
// Use precise ID filtering
var user = await context.Users
    .FirstOrDefaultAsync(u => u.Id == exactId);

// For range queries, ensure comparable format
var users = await context.Users
    .Where(u => u.Id.CompareTo("100") > 0 && u.Id.CompareTo("200") < 0)
    .ToListAsync();
```

## Transaction issues

### "Transaction already active"

**Error**: `InvalidOperationException: A transaction is already active...`

**Cause**: Trying to begin a transaction when one is already in progress.

**Solution**: Ensure you dispose or commit/rollback the previous transaction first.

### "Manifest not found" during recovery

**Cause**: Transaction state corruption or incomplete recovery.

**Solution**: This is rare. If it occurs:

1. Check storage access logs
2. Verify the transaction manifest files exist under `__cloudstorageorm/tx/`
3. Consider clearing failed manifests if they are orphaned

## Concurrency issues

### DbUpdateConcurrencyException on every update

**Cause**: ETag concurrency is enabled but entities are never retrieved with ETags properly set.

**Solution**:

1. Verify you're querying before updating:
   ```csharp
   var user = await context.Users.FirstOrDefaultAsync(u => u.Id == "123");
   user.Name = "Updated";
   await context.SaveChangesAsync(); // ETag should be valid
   ```

2. Do not manually set ETag; let CloudStorageORM manage it

### "Conflict detected" errors in production

**Cause**: High contention on frequently updated entities.

**Solution**:

1. Implement conflict resolution (see [Concurrency guide](concurrency.md))
2. Consider reducing update frequency
3. Consider cache-aside patterns for read-heavy workloads

## Performance issues

### Slow queries on large datasets

**Cause**: All results are materialized in memory.

**Solution**:

1. Prefer primary-key predicates when possible (non-key filters still materialize then filter):
   ```csharp
   // ❌ Slow: fetch all
   var users = (await context.Users.ToListAsync())
       .Where(u => u.Status == "Active");

   // ⚠️ Same non-key behavior here today (materialize + in-memory filter)
   var users = await context.Users
       .Where(u => u.Status == "Active")
       .ToListAsync();
   ```

2. Use range queries on primary keys for pagination
3. Consider batch operations for bulk updates

## Validation errors

### "Blob/object is too large"

**Cause**: Entity serializes to JSON larger than cloud storage limits.

**Solution**:

1. Reduce entity size: split into multiple entities
2. Compress data before storage (if supported by domain logic)
3. Store large fields separately

## Getting help

If you encounter issues not covered here:

1. Check the [GitHub Issues](https://github.com/rzavalik/CloudStorageORM/issues)
2. Review [API reference](api-reference.md) for detailed type info
3. Open a [Discussion](https://github.com/rzavalik/CloudStorageORM/discussions)
4. Report bugs with reproduction steps

## See also

- [Configuration](configuration.md)
- [Testing with Azurite](testing-with-azurite.md)
- [Testing with LocalStack](testing-with-localstack.md)