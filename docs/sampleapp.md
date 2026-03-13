# CloudStorageORM.SampleApp

`CloudStorageORM.SampleApp` demonstrates the current provider behavior on `main` by running the **same application flow** twice:

1. once with **EF Core InMemory**
2. once with **CloudStorageORM + Azure Blob Storage**

The sample currently targets **.NET 10** and uses **Azurite** by default for local Blob Storage emulation.

---

## What the sample proves

For both storage modes, the app executes the same steps:

1. list users
2. create a user
3. list users again
4. update the user
5. find the updated user through a LINQ query
6. delete the user
7. confirm the entity is gone

This is important because the goal of the sample is not just CRUD; it is demonstrating that the CloudStorageORM provider behaves close to familiar EF usage for the same application code.

---

## Current sample structure

| File | Purpose |
| :--- | :--- |
| `samples/CloudStorageORM.SampleApp/Program.cs` | Drives the two runs and prints the console output |
| `samples/CloudStorageORM.SampleApp/DbContext/MyAppDbContext.cs` | Defines the InMemory and CloudStorageORM contexts |
| `samples/CloudStorageORM.SampleApp/Models/User.cs` | Sample entity persisted to storage |

---

## Key behavior on the current branch

- The CloudStorageORM path is configured with `UseCloudStorageOrm(...)`.
- The sample uses `SaveChangesAsync()` for create, update, and delete.
- Listing uses `ToListAsync()`.
- Finding and updating use LINQ with `FirstOrDefault(...)`.
- The same domain model and CRUD flow are exercised for both providers.

---

## How to run locally

### Prerequisites

- .NET 10 SDK
- Docker (recommended, for Azurite)

### Start Azurite

```bash
docker run -d \
  -p 10000:10000 \
  -p 10001:10001 \
  -p 10002:10002 \
  --name azurite \
  mcr.microsoft.com/azure-storage/azurite
```

### Run the sample from the repository root

```bash
dotnet run --project samples/CloudStorageORM.SampleApp/SampleApp.csproj
```

---

## What you should expect to see

The console output is split into two sections:

- `Running using EF InMemory Provider...`
- `Running using EF CloudStorageOrm Provider...`

Both sections should complete successfully and finish with a message like:

```text
🏁 SampleApp Finished for MyAppDbContextInMemory.
🏁 SampleApp Finished for MyAppDbContextCloudStorage.
🏁 SampleApp Finished.
```

---

## Configuration details

The sample currently uses:

- provider: `CloudProvider.Azure`
- connection string: `UseDevelopmentStorage=true`
- container: `sampleapp-container`

If you want to point it to a real Azure Storage account instead of Azurite, update the CloudStorageORM configuration in `samples/CloudStorageORM.SampleApp/Program.cs`.

---

## Regression safety

This sample is also covered by an integration test:

- `tests/CloudStorageORM.IntegrationTests/ProgramExitTests.cs`

That test launches the sample through `dotnet run` and verifies it exits with code `0` and prints `SampleApp Finished`.

---

## Why this sample matters

The sample is the clearest executable proof, on the current branch, that CloudStorageORM is intended to be consumed like an EF provider rather than through a completely separate repository API.
It also serves as a guardrail for query execution changes, provider behavior, and end-to-end startup wiring.

---

## Related docs

- [README](../README.md)
- [Library documentation](./CloudStorageORM.md)
- [Testing with Azurite](./testing-with-azurite.md)


