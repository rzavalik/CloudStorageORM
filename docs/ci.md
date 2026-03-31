# CI workflow (`.github/workflows/ci.yml`)

This repository validates build, tests, and coverage through the `Build and Test` workflow.
Package publishing is handled by `.github/workflows/publish.yml`, which validates that the packed NuGet includes `README.md` and correct repository/readme metadata before push.

---

## Triggers

The workflow runs on:

- push to `main`
- pull request targeting `main`, `feature/**`, `bug/**`, or `hotfix/**`

---

## What CI does

In order, CI executes:

1. checkout
2. NuGet cache restore/save
3. .NET SDK setup (`10.0.x`)
4. `dotnet restore CloudStorageORM.sln`
5. `dotnet build CloudStorageORM.sln --no-restore --configuration Release`
6. start Azurite container
7. start LocalStack container (S3 enabled)
8. wait for emulators
9. run all `*.Tests.csproj` projects with TRX + XPlat coverage
10. upload TRX artifacts
11. upload Cobertura XML artifacts
12. generate HTML coverage report
13. upload HTML coverage artifact
14. publish PR test comment and unit test UI results
15. cleanup containers

---

## Emulator details

CI starts:

- Azurite on `10000`, `10001`, `10002`
- LocalStack (`localstack/localstack:3`) on `4566`

AWS test environment variables are injected in CI:

- `CLOUDSTORAGEORM_AWS_SERVICE_URL=http://127.0.0.1:4566`
- `CLOUDSTORAGEORM_AWS_ACCESS_KEY_ID=test`
- `CLOUDSTORAGEORM_AWS_SECRET_ACCESS_KEY=test`
- `CLOUDSTORAGEORM_AWS_REGION=us-east-1`
- `CLOUDSTORAGEORM_AWS_BUCKET=cloudstorageorm-integration-tests`

---

## Test and coverage artifacts

CI publishes three artifacts:

- `test-results` -> `TestResults/*.trx`
- `coverage-xml` -> `TestResults/Coverage/*.xml`
- `coverage-html` -> `TestResults/CoverageReport`

The workflow also publishes test results to the GitHub Actions UI on every run.

---

## Local parity commands

If you want local behavior close to CI:

```bash
dotnet restore CloudStorageORM.sln
dotnet build CloudStorageORM.sln --no-restore --configuration Release --verbosity normal

mkdir -p TestResults/Coverage
for proj in $(find . -type f -iname "*.Tests.csproj"); do
  name=$(basename "$proj" .csproj)
  dotnet test "$proj" --configuration Release \
    --logger "trx;LogFileName=${name}.trx" \
    --collect:"XPlat Code Coverage"
  find . -name "${name}.trx" -exec cp {} TestResults/ \;
  find . -name "coverage.cobertura.xml" -exec cp {} TestResults/Coverage/${name}.xml \;
done
```

Then generate HTML coverage:

```bash
dotnet tool restore
dotnet tool run reportgenerator \
  -reports:TestResults/Coverage/*.xml \
  -targetdir:TestResults/CoverageReport \
  -reporttypes:HtmlInline_AzurePipelines\;Cobertura
```

---

## Troubleshooting CI failures quickly

- If Azure tests fail early, verify Azurite readiness and port collisions.
- If AWS tests fail early, verify LocalStack health and `CLOUDSTORAGEORM_AWS_*` values.
- If report generation fails, verify `dotnet-tools.json` is restored (`dotnet tool restore`).
- If test reporting fails, inspect generated `TestResults/*.trx` files first.

