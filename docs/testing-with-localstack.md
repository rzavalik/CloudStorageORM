# Testing CloudStorageORM with LocalStack

CloudStorageORM uses [LocalStack](https://www.localstack.cloud/) to emulate AWS S3 locally.
Use this setup to run AWS integration tests and the AWS sample app flow without a real AWS account.

---

## Prerequisites

- .NET 10 SDK
- Docker

---

## Start LocalStack (S3)

```bash
docker rm -f localstack || true
docker run -d \
  -p 4566:4566 \
  --name localstack \
  -e SERVICES=s3 \
  -e AWS_DEFAULT_REGION=us-east-1 \
  localstack/localstack:3
```

---

## Environment variables used by CloudStorageORM

These defaults are used by tests/sample if variables are not set:

- `CLOUDSTORAGEORM_AWS_ACCESS_KEY_ID` (`test`)
- `CLOUDSTORAGEORM_AWS_SECRET_ACCESS_KEY` (`test`)
- `CLOUDSTORAGEORM_AWS_REGION` (`us-east-1`)
- `CLOUDSTORAGEORM_AWS_SERVICE_URL` (`http://localhost:4566`)
- `CLOUDSTORAGEORM_AWS_BUCKET` (`cloudstorageorm-integration-tests`)
- `CLOUDSTORAGEORM_AWS_FORCE_PATH_STYLE` (`true`)

---

## Run AWS integration tests only

```bash
dotnet test tests/CloudStorageORM.IntegrationTests/CloudStorageORM.IntegrationTests.Azure.csproj \
  --filter "FullyQualifiedName~Aws" \
  --nologo -v minimal
```

Note: there is a single integration test project file (`CloudStorageORM.IntegrationTests.Azure.csproj`) that contains both Azure and AWS integration tests.

If LocalStack is unavailable, AWS integration scenarios are skipped by fixture guards.

---

## Run the sample app with LocalStack available

```bash
dotnet run --project samples/CloudStorageORM.SampleApp/SampleApp.csproj
```

The sample executes three runs in order:

1. InMemory
2. Azure
3. AWS

If LocalStack is unreachable, the AWS run is skipped with a warning message.

---

## Common issues

### LocalStack not reachable

```bash
curl -sS http://127.0.0.1:4566/_localstack/health
```

You should see `"s3": "running"` in the response.

### Wrong endpoint or credentials

Set explicit variables before running tests/sample:

```bash
export CLOUDSTORAGEORM_AWS_ACCESS_KEY_ID=test
export CLOUDSTORAGEORM_AWS_SECRET_ACCESS_KEY=test
export CLOUDSTORAGEORM_AWS_REGION=us-east-1
export CLOUDSTORAGEORM_AWS_SERVICE_URL=http://localhost:4566
export CLOUDSTORAGEORM_AWS_BUCKET=cloudstorageorm-integration-tests
export CLOUDSTORAGEORM_AWS_FORCE_PATH_STYLE=true
```

---

## Related files

- `src/CloudStorageORM/Providers/Aws/StorageProviders/AwsS3StorageProvider.cs`
- `tests/CloudStorageORM.IntegrationTests/Aws/LocalStackFixture.cs`
- `tests/CloudStorageORM.IntegrationTests/Aws/StorageProviders/AwsS3StorageProviderTests.cs`
- `tests/CloudStorageORM.IntegrationTests/Aws/ProgramExitAwsTests.cs`
