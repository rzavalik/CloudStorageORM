# Contributing to CloudStorageORM

Thank you for considering contributing to **CloudStorageORM**.
This document reflects the current expectations for the `main` branch.

---

## ✅ Quick checklist

Before opening a PR, make sure you have:

- [ ] built with the **.NET 10 SDK**
- [ ] formatted the solution with `dotnet format`
- [ ] run the relevant tests
- [ ] updated documentation when changing public behavior, supported platforms, or contributor workflows

---

## Local prerequisites

For active development on the current branch, you should have:

- .NET 10 SDK
- Docker (recommended for Azurite- and LocalStack-backed integration tests)

---

## How to contribute

- Fork the repository.
- Create a branch for your work following the naming convention below.
- Implement your change.
- Run formatting and tests locally.
- Open a Pull Request referencing the related issue.

---

## Branch naming convention

| Type | Prefix | Example |
| :--- | :--- | :--- |
| New Feature | `feature/` | `feature/implement-azureprovider` |
| Bug Fix | `bug/` | `bug/fix-query-evaluation` |
| Hotfix | `hotfix/` | `hotfix/fix-release-blocker` |
| Tests | `test/` | `test/add-sampleapp-exit-coverage` |
| Documentation | `docs/` | `docs/update-net10-guidance` |
| Refactoring | `refactor/` | `refactor/simplify-query-provider` |

Examples:

- `feature/create-istorageprovider`
- `bug/fix-path-handling`
- `hotfix/fix-ci-breakage`
- `test/unit-azureblobstorageprovider`

---

## Pull request guidelines

### PR title format

Use:

```text
[TYPE] Short Description
```

Where `TYPE` is one of:

- `[FEATURE]`
- `[BUGFIX]`
- `[TEST]`
- `[DOCS]`
- `[REFACTOR]`

Examples:

- `[FEATURE] Implement AzureBlobStorageProvider`
- `[BUGFIX] Fix LINQ execution for single-entity queries`
- `[TEST] Add sample app process integration test`

### PR description

Please include:

- a short summary of what changed
- why the change was needed
- `Closes #issue_number` when applicable
- any local setup notes reviewers need (for example, Azurite or LocalStack)

---

## Coding style requirements

The repository formatting rules are driven by `.editorconfig`.
The current branch expects, among other things:

- **file-scoped namespaces**: `namespace My.Namespace;`
- `using` directives **outside** namespaces
- `var` usage preferred in the configured style rules
- standard solution-wide formatting via `dotnet format`

Run:

```bash
dotnet format CloudStorageORM.sln --verbosity minimal
```

---

## Testing expectations

### Unit + integration tests

```bash
./scripts/run-local-ci-tests.sh
```

This script mirrors CI startup/test behavior and runs Azurite with `--skipApiVersionCheck` for local compatibility.

### Integration tests with Azurite

If your change touches Azure provider behavior, sample app behavior, or end-to-end query execution, start Azurite first:

```bash
docker run -d \
  -p 10000:10000 \
  -p 10001:10001 \
  -p 10002:10002 \
  --name azurite \
  mcr.microsoft.com/azure-storage/azurite:latest \
  azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --skipApiVersionCheck
```

### Integration tests with LocalStack (AWS)

If your change touches AWS provider behavior, start LocalStack with S3 enabled:

```bash
docker run -d \
  -p 4566:4566 \
  --name localstack \
  -e SERVICES=s3 \
  -e AWS_DEFAULT_REGION=us-east-1 \
  localstack/localstack:3
```

### Coverage workflow

```bash
dotnet test CloudStorageORM.sln --nologo --settings coverlet.runsettings --collect:"XPlat Code Coverage" -v minimal
dotnet tool restore
dotnet tool run reportgenerator \
  -reports:"tests/**/TestResults/*/coverage.cobertura.xml" \
  -targetdir:"coverage/report" \
  -reporttypes:"Html"
```

---

## Documentation expectations

Documentation updates are required when your PR changes any of the following:

- target framework or SDK expectations
- supported providers
- public namespaces or public API shape
- sample app behavior
- test, coverage, or local development workflow

The most likely files to update are:

- `README.md`
- `docs/CloudStorageORM.md`
- `docs/sampleapp.md`
- `docs/testing-with-azurite.md`
- `docs/testing-with-localstack.md`
- `docs/ci.md`
- `.github/copilot-instructions.md`
- `ROADMAP.md`

---

## Pull request checklist

Before submitting your PR, verify:

- [ ] The branch name follows the convention.
- [ ] The PR title follows the convention.
- [ ] The relevant tests pass.
- [ ] The solution is formatted.
- [ ] Documentation was updated if behavior changed.
- [ ] Related issues are referenced.

---

## Branch protection

Direct pushes to `main` are not allowed.
All changes should go through a PR and pass CI.

Current CI (`.github/workflows/ci.yml`) runs on:

- pushes to `main`
- pull requests targeting `main`, `feature/**`, `bug/**`, or `hotfix/**`

Release publishing (`.github/workflows/publish.yml`) runs on `v*.*.*` tags (or manual dispatch) and publishes `CloudStorageORM` to NuGet.org and GitHub Packages.

CI and publish workflows currently opt JavaScript-based GitHub Actions into the Node.js 24 runtime using `FORCE_JAVASCRIPT_ACTIONS_TO_NODE24=true`.

CI also exports a CycloneDX SBOM artifact (`sbom-cyclonedx`) for each run.

If you change CI behavior, update contributor and testing docs in the same PR.

---

## Need help?

Open a [Discussion](https://github.com/rzavalik/CloudStorageORM/discussions) if you have questions, ideas, or design feedback.
