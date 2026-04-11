# CloudStorageORM

**Simplify persistence. Embrace scalability. Build the future.**

CloudStorageORM is an Entity Framework-style provider that persists entities into cloud object storage.
The current `main` branch targets **.NET 10**, uses **EF Core 9**, and currently ships with **Azure Blob Storage** and *
*AWS S3** providers.
Support for **Google Cloud Storage** remains on the roadmap.

![License](https://img.shields.io/badge/license-GPLv3-blue)
![.NET](https://img.shields.io/badge/.NET-10.0-blue)
![NuGet](https://img.shields.io/nuget/v/CloudStorageORM?color=blue)
![Build Status](https://github.com/rzavalik/CloudStorageORM/actions/workflows/ci.yml/badge.svg)
![Publish Status](https://github.com/rzavalik/CloudStorageORM/actions/workflows/publish.yml/badge.svg)
[![Contributing](https://img.shields.io/badge/Contributing-Guidelines-blue.svg)](./CONTRIBUTING.md)
[![Security Policy](https://img.shields.io/badge/Security-Policy-blue.svg)](./SECURITY.md)

[👉 See the roadmap](./ROADMAP.md)

---

## ✨ Current status

- ✅ Current release line: `v1.0.12`
- ✅ Targets `net10.0`
- ✅ Azure Blob Storage provider is implemented
- ✅ AWS S3 provider is implemented
- ✅ EF-style `DbContext` integration via `UseCloudStorageOrm(...)`
- ✅ Sample app runs the same CRUD flow against EF InMemory, Azure, and AWS
- ✅ Unit + integration tests run locally with Azurite and LocalStack
- ✅ Coverage collection is wired with Coverlet + ReportGenerator
- 🚧 Google Cloud Storage provider is planned

---

## 📦 Installation

### From NuGet

```bash
dotnet add package CloudStorageORM
```

### From source (`main` branch)

The repository currently targets **.NET 10 SDK**.

```bash
git clone https://github.com/rzavalik/CloudStorageORM.git
cd CloudStorageORM
dotnet restore CloudStorageORM.sln
```

---

## 🚀 Getting started

The current recommended integration pattern is to configure a regular EF Core `DbContext` with
`UseCloudStorageOrm(...)`.

```csharp
using CloudStorageORM.Contexts;
using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : CloudStorageDbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(x => x.Id);
        base.OnModelCreating(modelBuilder);
    }
}

var services = new ServiceCollection();

services.AddDbContext<AppDbContext>(options =>
{
    options.UseCloudStorageOrm(storage =>
    {
        storage.Provider = CloudProvider.Azure;
        storage.ContainerName = "sampleapp-container";
        storage.Azure.ConnectionString = "UseDevelopmentStorage=true";
    });
});
```

Then use the context with familiar EF operations and LINQ:

```csharp
await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

var user = new User
{
    Id = Guid.NewGuid().ToString(),
    Name = "John Doe",
    Email = "john.doe@example.com"
};

db.Add(user);
await db.SaveChangesAsync();

var users = await db.Set<User>().ToListAsync();
var found = db.Set<User>().FirstOrDefault(x => x.Id == user.Id);

db.Remove(found!);
await db.SaveChangesAsync();
```

> Current provider support on `main`: **Azure Blob Storage** and **AWS S3**.

---

## 🧭 Important notes for the current branch

- The base context namespace is now `CloudStorageORM.Contexts`.
- Configuration uses composition on `CloudStorageOptions`: common fields stay on the root, while provider-specific
  fields are under `storage.Azure` and `storage.Aws`.
- `CloudStorageOptions.ConnectionString` was removed; use `storage.Azure.ConnectionString` for Azure configuration.
- Primary-key query predicates now support direct range-aware loading for `>`, `>=`, `<`, and `<=` in addition to
  equality-based lookups.
- Coding style is enforced with **file-scoped namespaces** (`namespace X;`).
- The sample app is covered by an integration test that verifies `dotnet run` exits successfully.
- Integration fixtures can skip Azure/AWS scenarios when Azurite/LocalStack are unavailable.
- CloudStorage transaction support now uses a durable transaction journal under
  `__cloudstorageorm/tx/<transactionId>/manifest.json`.
- `SaveChanges` during an active transaction stages durable operations in the manifest; `Commit` marks the manifest as
  committed and replays operations; `Rollback` marks the transaction as aborted.
- Each transaction has a unique `TransactionId` (`Guid`) and only one active transaction is allowed per `DbContext`
  instance.
- On startup of a new transaction manager instance, committed manifests are replayed and finalized (`Completed`), while
  pre-commit manifests are marked as aborted (`Aborted`).
- Opt-in optimistic concurrency is available through object-store ETags. Configure entities with
  `modelBuilder.Entity<TEntity>().UseObjectETagConcurrency()` (shadow `ETag`) or
  `UseObjectETagConcurrency(e => e.ETag)` (mapped property).
- Entities can optionally implement `IETag` to expose the current ETag value after materialization and successful saves;
  this interface is not required.
- When ETag concurrency is enabled, updates/deletes use provider-native `If-Match` conditions (Azure Blob and AWS S3)
  and conflicts are raised as `DbUpdateConcurrencyException`.
- Future versions may add provider-native temporary locking (for example, Azure blob leases and AWS
  conditional/object-lock strategies) to improve concurrent-writer coordination.
- `IDatabaseCreator` behavior is still minimal right now: schema-style database lifecycle methods are not fully
  implemented because object storage does not map 1:1 to relational database creation semantics.

---

## 🧪 Running tests locally

### CI-parity local run (recommended)

```bash
./scripts/run-local-ci-tests.sh
```

This script mirrors CI by starting Azurite and LocalStack, then running restore, build, and tests with TRX and coverage collection.

### Start Azurite (Azure integration)

```bash
docker rm -f azurite || true
docker run -d \
  -p 10000:10000 \
  -p 10001:10001 \
  -p 10002:10002 \
  --name azurite \
  mcr.microsoft.com/azure-storage/azurite:latest \
  azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --skipApiVersionCheck
```

### Run the solution tests

```bash
dotnet test CloudStorageORM.sln --nologo -v minimal
```

### Start LocalStack (AWS integration)

```bash
docker rm -f localstack || true
docker run -d \
  -p 4566:4566 \
  --name localstack \
  -e SERVICES=s3 \
  -e AWS_DEFAULT_REGION=us-east-1 \
  localstack/localstack:3
```

### Optional AWS environment overrides

The integration fixture uses defaults, but you can override them explicitly:

```bash
export CLOUDSTORAGEORM_AWS_SERVICE_URL=http://127.0.0.1:4566
export CLOUDSTORAGEORM_AWS_ACCESS_KEY_ID=test
export CLOUDSTORAGEORM_AWS_SECRET_ACCESS_KEY=test
export CLOUDSTORAGEORM_AWS_REGION=us-east-1
export CLOUDSTORAGEORM_AWS_BUCKET=cloudstorageorm-integration-tests
```

### Collect coverage

```bash
dotnet test CloudStorageORM.sln --nologo --settings coverlet.runsettings --collect:"XPlat Code Coverage" -v minimal
dotnet tool restore
dotnet tool run reportgenerator \
  -reports:"tests/**/TestResults/*/coverage.cobertura.xml" \
  -targetdir:"coverage/report" \
  -reporttypes:"Html"
```

The HTML report is generated at `coverage/report/index.html`.

For CI-equivalent behavior (per-test-project TRX and coverage artifacts), see [docs/ci.md](./docs/ci.md).

---

## 🧪 Running the sample app

```bash
dotnet run --project samples/CloudStorageORM.SampleApp/SampleApp.csproj
```

The app runs the same CRUD flow three times:

1. Once against EF Core InMemory
2. Once against CloudStorageORM configured for Azure Blob Storage / Azurite
3. Once against CloudStorageORM configured for AWS S3 / LocalStack

For CloudStorageORM runs, the sample also executes a transaction scenario:

- add entity inside a transaction and `Rollback` (entity should not persist)
- add entity inside a transaction and `Commit` (entity should persist)

See [docs/sampleapp.md](./docs/sampleapp.md) for details.

---

## 📚 Documentation

- [Library documentation](./docs/CloudStorageORM.md)
- [API reference (DocFX)](https://rzavalik.github.io/CloudStorageORM/api/)
- [Sample app guide](./docs/sampleapp.md)
- [Testing with Azurite](./docs/testing-with-azurite.md)
- [Testing with LocalStack](./docs/testing-with-localstack.md)
- [CI workflow and artifacts](./docs/ci.md)
- [Contributing](./CONTRIBUTING.md)
- [Roadmap](./ROADMAP.md)

---

## 🛡️ License

This project is licensed under the **GNU General Public License v3.0 (GPL-3.0-or-later)**.
See [LICENSE](./LICENSE) for details.

---

## 🤝 Contributing

Contributions are welcome.
Please read [CONTRIBUTING.md](./CONTRIBUTING.md) before opening a PR.

---


> _CloudStorageORM aims to make cloud object storage feel familiar to EF-oriented .NET applications, while staying
explicit about the current provider and platform limits._
