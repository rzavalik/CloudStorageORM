# Getting started

CloudStorageORM is an Entity Framework-style provider for .NET that persists entities into cloud object storage. This
guide will help you get up and running in minutes.

## Installation

Install the NuGet package:

```bash
dotnet add package CloudStorageORM
```

## Quick start (5 minutes)

### 1. Define your entity

```csharp
public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### 2. Create a DbContext

```csharp
using CloudStorageORM.Contexts;
using Microsoft.EntityFrameworkCore;

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

### 3. Configure for cloud storage

```csharp
using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddDbContext<AppDbContext>(options =>
{
    options.UseCloudStorageOrm(storage =>
    {
        storage.Provider = CloudProvider.Azure;
        storage.ContainerName = "my-app-container";
        storage.Azure.ConnectionString = "DefaultEndpointsProtocol=https;...";
    });
});
```

### 4. Use it like EF Core

```csharp
await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

// Create
var user = new User { Name = "Alice", Email = "alice@example.com" };
db.Add(user);
await db.SaveChangesAsync();

// Read
var users = await db.Users.ToListAsync();
var found = db.Users.FirstOrDefault(x => x.Id == user.Id);

// Update
user.Email = "alice.updated@example.com";
db.Update(user);
await db.SaveChangesAsync();

// Delete
db.Remove(user);
await db.SaveChangesAsync();
```

## Supported cloud providers

| Provider                 | Status        | Features                                  |
|--------------------------|---------------|-------------------------------------------|
| **Azure Blob Storage**   | ✅ Implemented | Full CRUD, transactions, ETag concurrency |
| **AWS S3**               | ✅ Implemented | Full CRUD, transactions, ETag concurrency |
| **Google Cloud Storage** | 🚧 Planned    | Coming in v1.3.0+                         |

## Next steps

- [Configuration guide](configuration.md) — Learn advanced configuration options
- [Transactions guide](transactions.md) — Understand transaction semantics
- [Concurrency guide](concurrency.md) — Implement optimistic concurrency with ETags
- [Sample app](sampleapp.md) — See a complete working example
- [API reference](api-reference.md) — Explore the full API