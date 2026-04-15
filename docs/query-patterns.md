# Query patterns

This guide covers common query patterns when using CloudStorageORM.

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

## Efficient queries

### Query by primary key

CloudStorageORM optimizes primary-key lookups:

```csharp
// Efficient: direct range-aware load
var user = await context.Users
    .FirstOrDefaultAsync(u => u.Id == "123");
```

### Range queries

Efficient range queries using comparison operators:

```csharp
// Load users with IDs > "100" and < "200"
var users = await context.Users
    .Where(u => u.Id > "100" && u.Id < "200")
    .ToListAsync();

// Supported operators: >, >=, <, <=
```

### Single entity retrieval

```csharp
// Using FirstOrDefault (efficient for PK)
var user = await context.Users
    .FirstOrDefaultAsync(u => u.Id == "123");

// Using Single (throws if not found)
var user = await context.Users
    .SingleAsync(u => u.Id == "123");
```

## Supported LINQ operations

| Operation          | Status | Notes                    |
|--------------------|--------|--------------------------|
| `Where()`          | ✅      | All conditions supported |
| `FirstOrDefault()` | ✅      | Optimized for PK queries |
| `Single()`         | ✅      | Optimized for PK queries |
| `ToList()`         | ✅      | Materializes all results |
| `ToListAsync()`    | ✅      | Async materialization    |
| `Count()`          | ✅      | Counts all entities      |
| `Any()`            | ✅      | Checks existence         |
| `Select()`         | ✅      | Projection supported     |

## Pagination

Since CloudStorageORM materializes results, implement pagination manually:

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

1. **Prefer range queries** over full materializations when possible
2. **Filter early** to reduce data transfer
3. **Use async methods** (`ToListAsync`, `FirstOrDefaultAsync`)
4. **Avoid multiple materializations** of the same query

## Limitations

- No server-side `Skip()` or `Take()`
- No `Include()` for related entities (object storage is not relational)
- Complex nested queries may require manual materialization

## See also

- [Getting started](getting-started.md)
- [Configuration](configuration.md)
- [API reference](api-reference.md)