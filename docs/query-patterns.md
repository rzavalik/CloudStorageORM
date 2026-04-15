# Query patterns

This guide explains how queries are executed today in CloudStorageORM, which patterns are optimized, and which patterns
fall back to in-memory evaluation.

## How query execution works (current behavior)

CloudStorageORM currently uses two execution paths:

1. **Primary-key optimized path**
    - Used when the predicate is recognized as a constraint on the entity primary key (`==`, `>`, `>=`, `<`, `<=`).
2. **In-memory path**
    - Used for non-key predicates or predicates that cannot be translated into a primary-key constraint.
    - CloudStorageORM loads entity objects from storage and applies LINQ operators in memory.

In practice, this means your assumption is correct: for non-key filtering, CloudStorageORM may need to enumerate and
materialize many objects before filtering.

## Basic queries

### Get all entities

```csharp
var allUsers = await context.Users.ToListAsync();
```

### Filter by condition

```csharp
var activeUsers = await context.Users
    .Where(u => u.Status == "Active")
    .ToListAsync();
```

> `Status` is a non-key property, so this query is evaluated in memory after loading entities.

## Primary-key optimized queries

### Query by primary key

CloudStorageORM optimizes direct primary-key equality lookups:

```csharp
// Efficient: direct range-aware load
var user = await context.Users
    .FirstOrDefaultAsync(u => u.Id == "123");
```

### Range queries

CloudStorageORM recognizes range constraints on the primary key using comparison operators:

```csharp
// Load users with IDs > "100" and < "200"
var users = await context.Users
    .Where(u => u.Id > "100" && u.Id < "200")
    .ToListAsync();

// Supported operators: >, >=, <, <=
```

Range queries are more efficient than full scans, but still depend on object listing. They are not equivalent to
relational
index seek/scan behavior.

### Single entity retrieval

```csharp
// Using FirstOrDefault (efficient for PK)
var user = await context.Users
    .FirstOrDefaultAsync(u => u.Id == "123");

// Using Single (throws if not found)
var user = await context.Users
    .SingleAsync(u => u.Id == "123");
```

## Non-key predicates (in-memory)

Predicates on non-key fields are currently evaluated in memory:

```csharp
var premiumUsers = await context.Users
    .Where(u => u.Plan == "Premium")
    .ToListAsync();
```

This pattern can require loading all objects for the entity type before applying the predicate.

## Practical guidance

1. Prefer primary-key equality for hot-path reads.
2. Prefer primary-key ranges over broad non-key filtering when possible.
3. For frequent non-key access patterns, maintain a separate lookup/projection model designed for key-based reads.
4. Use feature flags and incremental rollouts when moving relational query workloads to object storage.

## Supported LINQ operations

| Operation          | Supported | Optimized path | Notes                                                                     |
|--------------------|-----------|----------------|---------------------------------------------------------------------------|
| `Where()`          | ✅         | ⚠️ key-only    | Primary-key predicates can be optimized; non-key predicates run in memory |
| `FirstOrDefault()` | ✅         | ⚠️ key-only    | Optimized for recognized primary-key predicates                           |
| `Single()`         | ✅         | ⚠️ key-only    | Optimized for recognized primary-key predicates                           |
| `Any()`            | ✅         | ⚠️ key-only    | Non-key checks may require materialization                                |
| `Count()`          | ✅         | ⚠️ key-only    | Non-key counts may require materialization                                |
| `ToList()`         | ✅         | N/A            | Materializes query results                                                |
| `ToListAsync()`    | ✅         | N/A            | Async materialization                                                     |
| `Select()`         | ✅         | ⚠️ key-only    | Projection is applied after data is loaded                                |

## Pagination

CloudStorageORM currently does not push down `Skip`/`Take` to storage, so pagination is materialized and then sliced in
memory:

```csharp
var pageSize = 10;
var pageNumber = 2;

var users = await context.Users
    .ToListAsync();

var pagedUsers = users
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToList();
```

## Performance considerations

1. **Prefer primary-key filters** (`Id == ...`, key ranges) in latency-sensitive paths
2. **Treat non-key predicates as scan-like** in current implementation
3. **Use async methods** (`ToListAsync`, `FirstOrDefaultAsync`) to avoid blocking
4. **Avoid repeated full materialization** of the same entity set in request hot paths

## Limitations

- No server-side relational query execution; non-key filtering is in-memory
- No server-side `Skip()` or `Take()` pushdown
- No `Include()` for related entities (object storage is not relational)
- Composite-key optimization behavior is limited; optimize primarily for single primary-key constraints
- Complex nested queries may require manual materialization and reshaping

## See also

- [Getting started](getting-started.md)
- [Configuration](configuration.md)
- [API reference](api-reference.md)