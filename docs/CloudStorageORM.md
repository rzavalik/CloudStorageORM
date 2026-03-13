# 📦 CloudStorageORM - Library Documentation

**Target Framework**: `net10.0`  
**Language Version**: `C# 14`  
**EF Core Packages**: `Microsoft.EntityFrameworkCore 9.0.4`, `Microsoft.EntityFrameworkCore.Relational 9.0.4`  
**Testing**: `xUnit`, `Shouldly`, `Moq`, `Coverlet`, `ReportGenerator`

---

## Overview

`CloudStorageORM` is an Entity Framework-style provider that lets a .NET application persist entities into cloud object storage.
On the current `main` branch, the implemented storage provider is **Azure Blob Storage**.
The provider surface is designed so that **AWS S3** and **Google Cloud Storage** can be added later, but they are **not implemented yet**.

The current branch is focused on:

- EF-style `DbContext` integration
- LINQ query execution over persisted blobs
- CRUD flows that behave similarly to the EF InMemory provider for the sample app
- Unit and integration test coverage around infrastructure, queries, validators, and provider behavior

---

## Current project structure

| Path | Purpose |
| :--- | :--- |
| `src/CloudStorageORM/Contexts` | Base `CloudStorageDbContext` used by consuming applications |
| `src/CloudStorageORM/Extensions` | EF and DI extension methods such as `UseCloudStorageOrm(...)` |
| `src/CloudStorageORM/Infrastructure` | Query pipeline, database abstractions, type mapping, and EF integration internals |
| `src/CloudStorageORM/Interfaces` | Contracts for storage providers, validators, repositories, and infrastructure helpers |
| `src/CloudStorageORM/Options` | `CloudStorageOptions` configuration model |
| `src/CloudStorageORM/Providers/Azure` | Azure Blob Storage provider and validator implementation |
| `src/CloudStorageORM/Repositories` | Repository and queryable helpers |
| `src/CloudStorageORM/Validators` | Model and blob validation rules |
| `samples/CloudStorageORM.SampleApp` | Console sample that runs the same flow against InMemory and CloudStorageORM |
| `tests/CloudStorageORM.Tests` | Unit tests |
| `tests/CloudStorageORM.IntegrationTests` | Azurite-backed integration tests, including sample app process execution |

---

## Main public entry points

### `CloudStorageORM.Extensions.CloudStorageOrmExtensions`

Adds the provider to EF Core through:

- `UseCloudStorageOrm(this DbContextOptionsBuilder builder, Action<CloudStorageOptions>? configureOptions)`
- `UseCloudStorageOrm<TContext>(...)`

Use this when configuring a context with `AddDbContext(...)`.

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
- any other provider → `NotSupportedException`

---

## Query and persistence behavior

The current branch supports EF-style usage such as:

- `context.Add(entity)` / `context.Update(entity)` / `context.Remove(entity)`
- `await context.SaveChangesAsync()`
- `await context.Set<TEntity>().ToListAsync()`
- `context.Set<TEntity>().FirstOrDefault(predicate)`

The recent query work on `main` focuses on:

- evaluating LINQ queries directly instead of materializing everything and then searching in memory for single-entity lookups
- returning queryables and async enumerables compatible with EF-style execution
- keeping the sample app behavior aligned between EF InMemory and CloudStorageORM

---

## Provider status

### Implemented now

- Azure Blob Storage provider
- Azure blob validation
- Sample app demonstrating CRUD parity with EF InMemory
- Integration test verifying the sample app exits successfully

### Planned, not implemented yet

- AWS S3 provider
- Google Cloud Storage provider
- provider-specific locking and richer concurrency strategies
- broader snapshot/versioning support

---

## Important limitations to document clearly

These items are important for anyone consuming the current `main` branch:

1. **Only Azure is implemented today**  
   The enum contains more provider options, but `ProviderFactory` currently supports only Azure.

2. **Object storage is not relational storage**  
   Database creation / deletion semantics do not map directly to object storage.
   In particular, `CloudStorageDatabaseCreator` is still intentionally minimal and some methods remain placeholders or throw `NotImplementedException`.

3. **Current branch targets .NET 10**  
   Consumers building from source should use the .NET 10 SDK.

4. **Namespace style was standardized**  
   The codebase now uses file-scoped namespaces (`namespace X;`) and places `using` directives outside the namespace.

---

## Testing and coverage

The repository currently includes:

- unit tests in `tests/CloudStorageORM.Tests`
- Azurite-backed integration tests in `tests/CloudStorageORM.IntegrationTests`
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
The integration test `tests/CloudStorageORM.IntegrationTests/ProgramExitTests.cs` executes:

```bash
dotnet run --project samples/CloudStorageORM.SampleApp/SampleApp.csproj
```

and verifies the process exits successfully.

---

## Roadmap summary

Short term priorities still documented by the repository direction are:

- additional provider implementations
- better concurrency and locking semantics
- continued query behavior parity with familiar EF usage
- clearer production-readiness guidance per feature area

See [`ROADMAP.md`](../ROADMAP.md) for the broader plan.
