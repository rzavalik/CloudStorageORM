# Migration guide

This guide helps you migrate existing Entity Framework or in-memory applications to CloudStorageORM.

## Conceptual differences

### From EF Core (relational)

| Aspect        | EF Core (SQL)              | CloudStorageORM                     |
|---------------|----------------------------|-------------------------------------|
| Storage       | Relational tables          | Cloud object storage (blobs)        |
| Relationships | Foreign keys, joins        | Not supported; embed or denormalize |
| Transactions  | Full ACID                  | Durable journal + replay            |
| Queries       | Server-side SQL evaluation | In-memory LINQ evaluation           |
| Concurrency   | Row versioning             | ETag-based optimistic               |

### From EF InMemory

CloudStorageORM is designed to be **API-compatible** with EF InMemory, so most code transitions directly:

```csharp
// Works the same
var user = await context.Users.FirstOrDefaultAsync(u => u.Id == "123");
context.Add(newUser);
await context.SaveChangesAsync();
```

**Differences**:

- Data is persisted to cloud storage (not lost on restart)
- No relationships/navigation properties
- Limited LINQ support (no complex server-side queries)

## Step-by-step migration

### Step 1: Update DbContext

**Before** (EF Core or InMemory):

```csharp
public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase("test");
    }
}
```

**After** (CloudStorageORM):

```csharp
using CloudStorageORM.Contexts;

public class AppDbContext : CloudStorageDbContext
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(x => x.Id);
        base.OnModelCreating(modelBuilder);
    }
}
```

### Step 2: Remove relationships

**Before** (EF with foreign keys):

```csharp
public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } // Navigation property
}
```

**After** (CloudStorageORM - denormalized):

```csharp
public class Order
{
    public string Id { get; set; }
    public string UserId { get; set; } // Store ID, not full entity
    // Fetch related User separately if needed
}
```

### Step 3: Update queries

**Before** (EF with includes):

```csharp
var order = await context.Orders
    .Include(o => o.User)
    .FirstOrDefaultAsync(o => o.Id == orderId);
```

**After** (CloudStorageORM - fetch separately):

```csharp
var order = await context.Orders
    .FirstOrDefaultAsync(o => o.Id == orderId);

if (order != null)
{
    var user = await context.Users
        .FirstOrDefaultAsync(u => u.Id == order.UserId);
}
```

### Step 4: Simplify models

**Before** (Complex relational model):

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Order> Orders { get; set; }
    public Address Address { get; set; }
}
```

**After** (Flattened, self-contained):

```csharp
public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
}
```

### Step 5: Configure storage provider

**Before**:

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("test"));
```

**After**:

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseCloudStorageOrm(storage =>
    {
        storage.Provider = CloudProvider.Azure;
        storage.ContainerName = "my-app";
        storage.Azure.ConnectionString = Environment.GetEnvironmentVariable("AZURE_CONNECTION_STRING");
    }));
```

## Testing strategy

### During migration

Keep both implementations running in parallel:

```csharp
// Feature flag
var useCloudStorage = Environment.GetEnvironmentVariable("USE_CLOUD_STORAGE") == "true";

if (useCloudStorage)
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseCloudStorageOrm(storage => { /* ... */ }));
}
else
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("test"));
}
```

### Test both paths

```csharp
[Theory]
[InlineData(true)]  // CloudStorageORM
[InlineData(false)] // InMemory
public async Task TestUserPersistence(bool useCloudStorage)
{
    // Setup context based on useCloudStorage flag
    // Run same test against both providers
}
```

## Performance tuning

### From relational indexing

**Before** (SQL indexes):

```sql
CREATE INDEX idx_user_email ON users(email);
```

**After** (Query filtering in code):

```csharp
// Load and filter in-memory
var users = await context.Users.ToListAsync();
var byEmail = users.FirstOrDefault(u => u.Email == email);
```

### Caching strategy

For frequently accessed entities:

```csharp
private static readonly Dictionary<string, User> _userCache = new();

public async Task<User> GetUserAsync(string id)
{
    if (_userCache.TryGetValue(id, out var cachedUser))
        return cachedUser;

    var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
    if (user != null)
        _userCache[id] = user;

    return user;
}
```

## Rollback plan

If migration becomes problematic:

1. Keep feature flag enabled
2. Gradually shift traffic back to previous provider
3. Monitor error rates and performance
4. Complete migration in phases by entity type

## Data migration

### One-time export/import

```csharp
// Export from source
var allUsers = await sourceContext.Users.ToListAsync();

// Import to CloudStorageORM
foreach (var user in allUsers)
{
    cloudStorageContext.Add(user);
}
await cloudStorageContext.SaveChangesAsync();
```

### Continuous sync (during migration)

```csharp
public async Task SyncUser(User user)
{
    // Write to both
    sourceContext.Update(user);
    cloudStorageContext.Update(user);

    await sourceContext.SaveChangesAsync();
    await cloudStorageContext.SaveChangesAsync();
}
```

## Progressive live migration (planned)

> This section documents a proposed approach for a future release. It is not fully implemented on the current `main`
> branch.

For teams migrating a live relational table (for example, `Profiles`) to CloudStorageORM, the recommended future
direction is a
**progressive migration bridge** instead of a full stop-the-world cutover.

### Goals

- Keep the application online during migration
- Move data in phases by entity type
- Provide explicit rollback points at each phase
- Reduce risk with observability and drift checks

### Proposed architecture (future)

1. **Relational EF source context** remains write authority in early phases.
2. **Backfill worker** copies existing rows from database to cloud objects.
3. **Change capture + outbox** records profile updates/deletes in the same database transaction as the business write.
4. **Replication worker** replays outbox events into CloudStorageORM paths using existing provider abstractions.
5. **Read router** progressively shifts read traffic from database to cloud behind a feature flag.
6. **Shadow read validator** compares both sources during rollout to detect drift.

### Why this pattern

CloudStorageORM currently supports context-scoped durable transactions, but it does not provide distributed transactions
across
different data stores. Using an outbox-style bridge avoids assuming atomic commit between relational writes and
object-storage
writes while still allowing reliable, replayable synchronization.

### Rollout phases (future)

1. **Prepare**
    - Add migration feature flags.
    - Define idempotency key strategy (for example: `EntityId + Version` or `EntityId + UpdatedAt`).
2. **Backfill**
    - Copy all existing `Profiles` to cloud storage.
    - Track completion checkpoint for resumable execution.
3. **Dual-write async**
    - Keep database as source-of-truth.
    - Persist outbox records for every `Profiles` write/delete.
    - Replicate to cloud in background with retries.
4. **Shadow reads**
    - Serve reads from database.
    - Compare cloud result in background and emit drift metrics.
5. **Read cutover**
    - Gradually route reads to cloud by tenant, user segment, or percentage.
    - Keep automatic database fallback while confidence grows.
6. **Write cutover and decommission**
    - Promote cloud to primary write path.
    - Keep temporary mirror writes if needed.
    - Retire relational table only after stability window and reconciliation.

### Failure handling and rollback

- Pause rollout by feature flag without stopping the app.
- Keep outbox replay idempotent to tolerate retries and restarts.
- Route reads back to relational source immediately when drift/error budgets are exceeded.
- Prefer explicit failure and alerting over silent fallback for unsupported scenarios.

### Suggested future API shape (non-final)

```csharp
// Proposed API sketch for future releases (names are illustrative).
services.AddCloudStorageOrmMigrationBridge<AppDbContext>(bridge =>
{
    bridge.Entity<Profile>(entity =>
    {
        entity.HasKey(x => x.Id);
        entity.UseBackfill();
        entity.UseOutboxReplication();
        entity.EnableShadowReads();
    });

    bridge.ReadMode = MigrationReadMode.DatabasePrimary;
    bridge.WriteMode = MigrationWriteMode.DualWriteAsync;
});
```

Planned bridge behavior should continue to use standard `UseCloudStorageOrm(...)` configuration for cloud targets and
preserve
provider-specific options under `CloudStorageOptions.Azure` and `CloudStorageOptions.Aws`.

## See also

- [Getting started](getting-started.md)
- [Configuration](configuration.md)
- [Query patterns](query-patterns.md)
- [Transactions](transactions.md)