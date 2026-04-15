#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

AZURITE_IMAGE="mcr.microsoft.com/azure-storage/azurite:latest"
LOCALSTACK_IMAGE="localstack/localstack:3"

cleanup() {
  docker rm -f azurite localstack >/dev/null 2>&1 || true
}
trap cleanup EXIT

cleanup

echo "Starting Azurite with --skipApiVersionCheck..."
docker run -d \
  -p 10000:10000 \
  -p 10001:10001 \
  -p 10002:10002 \
  --name azurite \
  "$AZURITE_IMAGE" \
  azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --skipApiVersionCheck >/dev/null

echo "Starting LocalStack (S3)..."
docker run -d \
  -p 4566:4566 \
  -e SERVICES=s3 \
  -e AWS_DEFAULT_REGION=us-east-1 \
  --name localstack \
  "$LOCALSTACK_IMAGE" >/dev/null

sleep 8

export CLOUDSTORAGEORM_AWS_SERVICE_URL=http://127.0.0.1:4566
export CLOUDSTORAGEORM_AWS_ACCESS_KEY_ID=test
export CLOUDSTORAGEORM_AWS_SECRET_ACCESS_KEY=test
export CLOUDSTORAGEORM_AWS_REGION=us-east-1
export CLOUDSTORAGEORM_AWS_BUCKET=cloudstorageorm-integration-tests

mkdir -p TestResults/Coverage

echo "Restoring solution..."
dotnet restore CloudStorageORM.sln

echo "Building solution (Release)..."
dotnet build CloudStorageORM.sln --no-restore --configuration Release --verbosity normal

declare -a projects=()
while IFS= read -r project; do
  projects+=("$project")
done < <(find . -type f -iname "*.Tests.csproj" | sort)

# Keep local runs aligned with CI while including provider-specific integration projects.
integration_projects=(
  "./tests/CloudStorageORM.IntegrationTests/CloudStorageORM.IntegrationTests.Azure.csproj"
  "./tests/CloudStorageORM.IntegrationTests/CloudStorageORM.IntegrationTests.AWS.csproj"
  "./tests/CloudStorageORM.IntegrationTests.SampleApp/CloudStorageORM.IntegrationTests.SampleApp.csproj"
)

for integration_project in "${integration_projects[@]}"; do
  if [[ -f "$integration_project" ]]; then
    projects+=("$integration_project")
  fi
done

echo "Running test projects..."
for proj in "${projects[@]}"; do
  name="$(basename "$proj" .csproj)"
  echo "- $name ($proj)"

  dotnet test "$proj" --configuration Release \
    --logger "trx;LogFileName=${name}.trx" \
    --collect:"XPlat Code Coverage"

  find . -name "${name}.trx" -exec cp {} TestResults/ \;
  find . -name "coverage.cobertura.xml" -exec cp {} "TestResults/Coverage/${name}.xml" \;
done

echo "Done. TRX files are in TestResults/ and coverage XML files are in TestResults/Coverage/."
