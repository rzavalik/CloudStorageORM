# 🧪 Testing CloudStorageORM with Azurite

CloudStorageORM uses [Azurite](https://github.com/Azure/Azurite) to emulate Azure Blob Storage locally.
That is the expected local setup for the current `main` branch whenever you want to:

- run integration tests
- run the sample app in CloudStorageORM mode
- validate Azure Blob Storage behavior without a real Azure subscription

For AWS S3 local emulation, see [testing-with-localstack.md](./testing-with-localstack.md).

---

## Prerequisites

- .NET 10 SDK
- Docker

---

## Start Azurite

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

`--skipApiVersionCheck` keeps local emulator runs compatible when SDK defaults move to newer service API versions.

The repository uses the standard development connection string:

```text
UseDevelopmentStorage=true
```

---

## Verify Azurite is running

```bash
docker ps
```

You should see a container named `azurite` exposing ports `10000-10002`.

---

## Run the full test suite

From the repository root:

```bash
./scripts/run-local-ci-tests.sh
```

Or run tests directly:

```bash
dotnet test CloudStorageORM.sln --nologo -v minimal
```

This runs:

- unit tests in `tests/CloudStorageORM.Tests`
- Azure integration tests in `tests/CloudStorageORM.IntegrationTests/CloudStorageORM.IntegrationTests.Azure.csproj`
- AWS integration tests in `tests/CloudStorageORM.IntegrationTests/CloudStorageORM.IntegrationTests.AWS.csproj`
- SampleApp integration tests in
  `tests/CloudStorageORM.IntegrationTests.SampleApp/CloudStorageORM.IntegrationTests.SampleApp.csproj`

For CI-style TRX and coverage artifact layout, see [ci.md](./ci.md).

If Azurite is unavailable, Azure-backed integration scenarios are skipped by fixture guards.

---

## Run only the integration tests

```bash
dotnet test tests/CloudStorageORM.IntegrationTests/CloudStorageORM.IntegrationTests.Azure.csproj --nologo -v minimal
```

To focus only on the Azure transaction failure-window suite (`AzureTransactionFailureWindowTests`), which covers
rollback, commit, committed-manifest recovery, and stale-ETag conflict cases:

```bash
dotnet test tests/CloudStorageORM.IntegrationTests/CloudStorageORM.IntegrationTests.Azure.csproj --nologo -v minimal --filter "FullyQualifiedName~AzureTransactionFailureWindowTests"
```

The suite also asserts the transaction and concurrency log events emitted by the runtime path.

---

## Run the sample app against Azurite

```bash
dotnet run --project samples/CloudStorageORM.SampleApp/SampleApp.csproj
```

The sample app executes the same CRUD flow against:

1. EF InMemory
2. CloudStorageORM configured with Azure Blob Storage
3. CloudStorageORM configured with AWS S3

If Azurite is not reachable, the Azure run is skipped with a warning message.

---

## Collect coverage

Coverage collection is configured through `coverlet.runsettings` and the tool manifest in `dotnet-tools.json`.

### Collect Cobertura coverage files

```bash
dotnet test CloudStorageORM.sln --nologo --settings coverlet.runsettings --collect:"XPlat Code Coverage" -v minimal
```

Coverage files are emitted under each test project's `TestResults` directory.

### Generate an HTML report

```bash
dotnet tool restore
dotnet tool run reportgenerator \
  -reports:"tests/**/TestResults/*/coverage.cobertura.xml" \
  -targetdir:"coverage/report" \
  -reporttypes:"Html"
```

Open `coverage/report/index.html` in your browser.

You may see SourceLink 404 warnings from `reportgenerator` when local changes are not present in the remote commit
metadata; this does not block HTML report generation.

---

## Common issues

### Azurite is not running

Symptoms can include connection failures or integration tests timing out.
Start or restart the container:

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

### Port conflicts

Ensure ports `10000`, `10001`, and `10002` are free.

### You only want fast local verification

Run unit tests only:

```bash
dotnet test tests/CloudStorageORM.Tests/CloudStorageORM.Tests.csproj --nologo -v minimal
```

---

## Related files

- `tests/CloudStorageORM.IntegrationTests/CloudStorageORM.IntegrationTests.Azure.csproj`
- `tests/CloudStorageORM.IntegrationTests/Azure/StorageFixture.cs`
- `tests/CloudStorageORM.IntegrationTests/Azure/StorageProviders/AzureBlobStorageProviderTests.cs`
- `tests/CloudStorageORM.IntegrationTests/Azure/Transactions/AzureTransactionFailureWindowTests.cs`
- `coverlet.runsettings`
- `dotnet-tools.json`

---

## References

- [Azurite GitHub Repository](https://github.com/Azure/Azurite)
- [Azure Storage Emulator migration guidance](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-emulator)