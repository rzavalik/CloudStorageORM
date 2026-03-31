# 📦 CloudStorageORM - Library Documentation

**Target Framework**: `net10.0`  
**Language Version**: `C# 14`  
**EF Core Packages**: `Microsoft.EntityFrameworkCore 9.0.4`, `Microsoft.EntityFrameworkCore.Relational 9.0.4`  
**Testing**: `xUnit`, `Shouldly`, `Moq`, `Coverlet`, `ReportGenerator`

---

## Overview

`CloudStorageORM` is an Entity Framework-style provider that lets a .NET application persist entities into cloud object
storage.
On the current `main` branch, implemented storage providers are **Azure Blob Storage** and **AWS S3**.
The provider surface is designed so that **Google Cloud Storage** can be added later.

The current branch is focused on:

- EF-style `DbContext` integration
- LINQ query execution over persisted blobs
- CRUD flows that behave similarly to the EF InMemory provider for the sample app
- Unit and integration test coverage around infrastructure, queries, validators, and provider behavior

---

## Current project structure

| Path                                     | Purpose                                                                                  |
|:-----------------------------------------|:-----------------------------------------------------------------------------------------|
| `src/CloudStorageORM/Contexts`           | Base `CloudStorageDbContext` used by consuming applications                              |
| `src/CloudStorageORM/Extensions`         | EF and DI extension methods such as `UseCloudStorageOrm(...)`                            |
| `src/CloudStorageORM/Infrastructure`     | Query pipeline, database abstractions, type mapping, and EF integration internals        |
| `src/CloudStorageORM/Interfaces`         | Contracts for storage providers, validators, repositories, and infrastructure helpers    |
| `src/CloudStorageORM/Options`            | `CloudStorageOptions` configuration model                                                |
| `src/CloudStorageORM/Providers/Azure`    | Azure Blob Storage provider and validator implementation                                 |
| `src/CloudStorageORM/Providers/Aws`      | AWS S3 provider and validator implementation                                             |
| `src/CloudStorageORM/Repositories`       | Repository and queryable helpers                                                         |
| `src/CloudStorageORM/Validators`         | Model and blob validation rules                                                          |
| `samples/CloudStorageORM.SampleApp`      | Console sample that runs the same flow against InMemory and CloudStorageORM              |
| `tests/CloudStorageORM.Tests`            | Unit tests                                                                               |
| `tests/CloudStorageORM.IntegrationTests` | Azurite- and LocalStack-backed integration tests, including sample app process execution |

---

## Main public entry points

### `CloudStorageORM.Extensions.CloudStorageOrmExtensions`

Adds the provider to EF Core through:

- `UseCloudStorageOrm(this DbContextOptionsBuilder builder, Action<CloudStorageOptions>? configureOptions)`
- `UseCloudStorageOrm<TContext>(...)`

Use this when configuring a context with `AddDbContext(...)`.

Configuration model on the current branch:

- common fields: `CloudStorageOptions.Provider`, `CloudStorageOptions.ContainerName`
- Azure fields: `CloudStorageOptions.Azure.ConnectionString`
- AWS fields: `CloudStorageOptions.Aws.AccessKeyId`, `SecretAccessKey`, `Region`, `ServiceUrl`, `ForcePathStyle`
- `CloudStorageOptions.ConnectionString` at the root level is no longer used

### `CloudStorageORM.Contexts.CloudStorageDbContext`

Base context that:

- reads `CloudStorageOptions` from EF options
- resolves the correct storage provider through `ProviderFactory`
- applies blob settings conventions
- validates the EF model against storage constraints

### `CloudStorageORM.Extensions.CloudStorageOrmServiceCollectionExtensions`

Adds related services to DI through:

- `AddEntityFrameworkCloudStorageOrm(this IServiceCollection services, CloudStorageOptions storageOptions)`

### `CloudStorageORM.Providers.ProviderFactory`

Current behavior:

- `CloudProvider.Azure` → `AzureBlobStorageProvider`
- `CloudProvider.Aws` → `AwsS3StorageProvider`
- any other provider → `NotSupportedException`

---

## Query and persistence behavior

The current branch supports EF-style usage such as:

- `context.Add(entity)` / `context.Update(entity)` / `context.Remove(entity)`
- `await context.SaveChangesAsync()`
- `await context.Set<TEntity>().ToListAsync()`
- `context.Set<TEntity>().FirstOrDefault(predicate)`

Transaction behavior on the current branch:

- `BeginTransaction()` opens a context-scoped transaction manager.
- Every transaction gets a unique `TransactionId` (`Guid`).
- `SaveChanges()` inside an active transaction appends staged operations to `__cloudstorageorm/tx/<transactionId>/manifest.json` with state `Preparing`.
- `Commit()` marks the manifest as `Committed`, replays operations in sequence, and then marks it as `Completed`.
- `Rollback()` (or disposing an uncommitted transaction) marks the manifest as `Aborted`.
- Recovery scans `__cloudstorageorm/tx/` when a new transaction manager starts: committed manifests are replayed/finalized; preparing manifests are aborted.

The recent query work on `main` focuses on:

- evaluating LINQ queries directly instead of materializing everything and then searching in memory for single-entity
  lookups
- returning queryables and async enumerables compatible with EF-style execution
- keeping the sample app behavior aligned between EF InMemory and CloudStorageORM

---

## Provider status

### Implemented now

- Azure Blob Storage provider
- AWS S3 provider
- Azure blob validation
- AWS object validation
- Sample app demonstrating CRUD parity with EF InMemory, Azure, and AWS
- Integration tests verifying storage flows and sample app process exit

### Planned, not implemented yet

- Google Cloud Storage provider
- provider-native temporary locking and richer concurrency strategies (for example, Azure blob lease coordination and AWS conditional/object-lock patterns)
- broader snapshot/versioning support

---

## Important limitations to document clearly

These items are important for anyone consuming the current `main` branch:

1. **Azure and AWS are implemented today**  
   `ProviderFactory` currently supports Azure and AWS. GCP remains planned.

2. **Object storage is not relational storage**  
   Database creation / deletion semantics do not map directly to object storage.
   In particular, `CloudStorageDatabaseCreator` is still intentionally minimal and some methods remain placeholders or
   throw `NotImplementedException`.

   Transactions are implemented through a provider-level journal and replay mechanism; this is still not a full relational ACID engine or a distributed lock/consensus coordinator.
   Provider-native temporary locking is planned for future versions but is not part of the current transaction guarantee.

3. **Current branch targets .NET 10**  
   Consumers building from source should use the .NET 10 SDK.

4. **Namespace style was standardized**  
   The codebase now uses file-scoped namespaces (`namespace X;`) and places `using` directives outside the namespace.

---

## Testing and coverage

The repository currently includes:

- unit tests in `tests/CloudStorageORM.Tests`
- Azurite- and LocalStack-backed integration tests in `tests/CloudStorageORM.IntegrationTests`
- coverage collection through `coverlet.collector` / `coverlet.msbuild`
- HTML report generation through the local tool manifest in `dotnet-tools.json`

Typical commands:

```bash
dotnet test CloudStorageORM.sln --nologo -v minimal
dotnet test CloudStorageORM.sln --nologo --settings coverlet.runsettings --collect:"XPlat Code Coverage" -v minimal
dotnet tool restore
dotnet tool run reportgenerator -reports:"tests/**/TestResults/*/coverage.cobertura.xml" -targetdir:"coverage/report" -reporttypes:"Html"
```

---

## Sample app relationship

The sample app is not just a demo now; it is also part of the regression safety net.
The integration tests `tests/CloudStorageORM.IntegrationTests/ProgramExitTests.cs` and
`tests/CloudStorageORM.IntegrationTests/Aws/ProgramExitAwsTests.cs` execute:

```bash
dotnet run --project samples/CloudStorageORM.SampleApp/SampleApp.csproj
```

and verify the process exits successfully.

---

## Roadmap summary

Short term priorities still documented by the repository direction are:

- GCP provider implementation
- better concurrency and locking semantics
- continued query behavior parity with familiar EF usage
- clearer production-readiness guidance per feature area

See [`ROADMAP.md`](../ROADMAP.md) for the broader plan.
