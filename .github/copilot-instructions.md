# Copilot Instructions for CloudStorageORM

These instructions apply repository-wide and are intended to keep generated changes aligned with the current `main` branch behavior.

## Project context

- `CloudStorageORM` is an EF-style provider over object storage.
- Current supported providers: Azure Blob Storage and AWS S3.
- Target framework is `.NET 10` (`net10.0`).
- Google Cloud Storage is not implemented yet.

## Code style and boundaries

- Follow `.editorconfig` and use file-scoped namespaces (`namespace X;`).
- Keep `using` directives outside namespaces.
- Prefer focused changes over broad refactors.
- Do not introduce new provider-specific behavior in shared abstractions unless required.

## Configuration expectations

- Use `UseCloudStorageOrm(...)` patterns used in the repository.
- Keep common options on `CloudStorageOptions` root.
- Keep provider-specific options under `CloudStorageOptions.Azure` and `CloudStorageOptions.Aws`.
- Do not use removed root `ConnectionString` patterns.

## Testing expectations

- Run relevant tests for changed areas.
- If Azure behavior changes, validate with Azurite-backed integration tests.
- If AWS behavior changes, validate with LocalStack-backed integration tests.
- Keep test changes deterministic and avoid hidden environment coupling.

## CI alignment

- Align commands and assumptions with `.github/workflows/ci.yml`.
- CI runs restore, build, tests, and coverage artifact generation.
- CI uses Azurite and LocalStack (`localstack/localstack:3`).
- Prefer `127.0.0.1` for local endpoints in docs and examples when matching CI.

## Documentation update rules

When a PR changes behavior, update docs in the same PR.

Common mappings:

- Public API or configuration changes -> `README.md` and `docs/CloudStorageORM.md`
- Local test workflow changes -> `docs/testing-with-azurite.md` and `docs/testing-with-localstack.md`
- CI workflow changes -> `docs/ci.md` and `CONTRIBUTING.md`
- Contributor process changes -> `CONTRIBUTING.md`

## PR and branch conventions

- Use branch prefixes documented in `CONTRIBUTING.md` (`feature/`, `bug/`, `hotfix/`, `test/`, `docs/`, `refactor/`).
- Keep PR titles and descriptions consistent with `CONTRIBUTING.md`.

## Guardrails

- Do not remove tests to make CI pass.
- Do not change target framework or package strategy without updating docs.
- Prefer explicit errors over silent fallbacks for unsupported provider behavior.

_Last reviewed against docs and CI: 2026-03-31._

