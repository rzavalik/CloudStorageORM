# CloudStorageORM.SampleApp

`CloudStorageORM.SampleApp` demonstrates the current provider behavior on `main` by running the **same application flow** three times:

1. once with **EF Core InMemory**
2. once with **CloudStorageORM + Azure Blob Storage**
3. once with **CloudStorageORM + AWS S3**

The sample currently targets **.NET 10** and uses **Azurite** + **LocalStack** by default for local cloud emulation.

---

## What the sample proves

For all storage modes, the app executes the same steps:

1. list users
2. create a user
3. list users again
4. update the user
5. find the updated user through a LINQ query
6. delete the user
7. confirm the entity is gone

For CloudStorageORM-backed runs (Azure and AWS), the app also verifies transaction semantics:

8. add a user inside a transaction and roll back (must not persist)
9. add a user inside a transaction and commit (must persist)

This scenario validates transaction staging/commit behavior backed by the durable journal under `__cloudstorageorm/tx/`.

This is important because the goal of the sample is not just CRUD; it is demonstrating that the CloudStorageORM provider behaves close to familiar EF usage for the same application code.

---

## Current sample structure

| File | Purpose |
| :--- | :--- |
| `samples/CloudStorageORM.SampleApp/Program.cs` | Drives the three runs and prints the console output |
| `samples/CloudStorageORM.SampleApp/DbContext/MyAppDbContext.cs` | Defines the InMemory and CloudStorageORM contexts |
| `samples/CloudStorageORM.SampleApp/Models/User.cs` | Sample entity persisted to storage |

---

## Key behavior on the current branch

- The CloudStorageORM path is configured with `UseCloudStorageOrm(...)`.
- The sample uses `SaveChangesAsync()` for create, update, and delete.
- Listing uses `ToListAsync()`.
- Finding and updating use LINQ with `FirstOrDefault(...)`.
- The same domain model and CRUD flow are exercised for all providers.
- Each run clears existing users first to avoid leftover objects from previous executions.
- The sample uses a deterministic user ID (`sample-user-001`) for predictable output and easier assertions.

---

## How to run locally

### Prerequisites

- .NET 10 SDK
- Docker (recommended, for Azurite + LocalStack)

### Start Azurite

```bash
docker run -d \
  -p 10000:10000 \
  -p 10001:10001 \
  -p 10002:10002 \
  --name azurite \
  mcr.microsoft.com/azure-storage/azurite
```

### Start LocalStack (S3)

```bash
docker run -d \
  -p 4566:4566 \
  --name localstack \
  -e SERVICES=s3 \
  -e AWS_DEFAULT_REGION=us-east-1 \
  localstack/localstack:3
```

### Run the sample from the repository root

```bash
dotnet run --project samples/CloudStorageORM.SampleApp/SampleApp.csproj
```

---

## What you should expect to see

The console output is split into three sections:

- `Running using EF InMemory Provider...`
- `Running using EF Azure Provider...`
- `Running using EF Aws Provider...`

All sections should complete successfully and finish with messages like:

```text
🏁 SampleApp Finished for MyAppDbContextInMemory.
🏁 SampleApp Finished for MyAppDbContextCloudStorage.
🏁 SampleApp Finished for MyAppDbContextCloudStorage.
🏁 SampleApp Finished.
```

---

## Configuration details

The sample currently uses environment-based configuration with defaults:

- Azure connection string: `UseDevelopmentStorage=true`
- Azure container: `sampleapp-container`
- AWS endpoint: `http://localhost:4566`
- AWS region: `us-east-1`
- AWS credentials: `test` / `test`

You can override values with env vars (for example `CLOUDSTORAGEORM_AZURE_CONNECTION_STRING`, `CLOUDSTORAGEORM_AWS_SERVICE_URL`, `CLOUDSTORAGEORM_AWS_BUCKET`).

---

## Regression safety

This sample is also covered by an integration test:

- `tests/CloudStorageORM.IntegrationTests/ProgramExitTests.cs`
- `tests/CloudStorageORM.IntegrationTests/Aws/ProgramExitAwsTests.cs`

Those tests launch the sample through `dotnet run` and verify it exits with code `0` and prints `SampleApp Finished`.

---

## Why this sample matters

The sample is the clearest executable proof, on the current branch, that CloudStorageORM is intended to be consumed like an EF provider rather than through a completely separate repository API.
It also serves as a guardrail for query execution changes, provider behavior, and end-to-end startup wiring.

---

## Related docs

- [README](../README.md)
- [Library documentation](./CloudStorageORM.md)
- [Testing with Azurite](./testing-with-azurite.md)
- [Testing with LocalStack](./testing-with-localstack.md)


