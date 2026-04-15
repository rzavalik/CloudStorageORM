# Documentation overview

This directory contains all documentation for CloudStorageORM, auto-published to GitHub Pages via DocFX.

## Content structure

### 🎯 User guides (for application developers)

- **[Getting started](getting-started.md)** — Quick setup and first steps (5 min)
- **[Configuration](configuration.md)** — Connect to Azure/AWS, configure options
- **[Query patterns](query-patterns.md)** — LINQ queries, filtering, pagination
- **[Transactions](transactions.md)** — Durable transactions with recovery
- **[Concurrency](concurrency.md)** — Optimistic locking with ETags
- **[Providers](providers.md)** — Provider-specific guidance (Azure, AWS, GCP)
- **[Migration guide](migration.md)** — Migrate from EF Core or EF InMemory
- **[Troubleshooting](troubleshooting.md)** — Common issues and solutions

### 📚 Reference (for library users and maintainers)

- **[Library documentation](CloudStorageORM.md)** — Architecture, project structure, public APIs
- **[Sample application](sampleapp.md)** — End-to-end CRUD example
- **[API reference](api-reference.md)** — Auto-generated API docs (xref links)

### 🧪 Testing & operations

- **[Testing with Azurite](testing-with-azurite.md)** — Local Azure Blob Storage testing
- **[Testing with LocalStack](testing-with-localstack.md)** — Local AWS S3 testing
- **[CI workflow](ci.md)** — GitHub Actions pipeline documentation

### 📋 Root-level docs (included in site)

These are auto-included from the repo root and published to the site:

- `README.md` — Project overview
- `ROADMAP.md` — Feature roadmap and release milestones
- `CONTRIBUTING.md` — Contribution guidelines
- `SECURITY.md` — Security policy and reporting
- `LICENSE` — GPL-3.0 license

## Navigation

The **[table of contents (toc.yml)](toc.yml)** controls the left-side navigation menu. Edit this file to reorganize docs
or add new sections.

The **[index.md](index.md)** is the home page, with quick links and status table.

## Building locally

### Prerequisites

```bash
# .NET 10 SDK
dotnet --version

# DocFX and docfx-material template
dotnet tool update --global docfx
./scripts/setup-docfx-material.sh
```

### Build the site

```bash
cd /repo/root
./scripts/setup-docfx-material.sh
docfx docfx.json

# Open in browser
open _site/index.html
```

### Live preview (development)

If using an editor with live markdown preview, files in `docs/` can be previewed locally during writing.

## Publishing

The site is **auto-published** by GitHub Actions on pushes to `main`, which deploys to the `gh-pages` branch.

See [CI workflow](ci.md) for documentation, or view the configuration at `.github/workflows/ci.yml` in the repo.

### Manual checks before merge

```bash
./scripts/setup-docfx-material.sh
docfx docfx.json
open _site/index.html  # Review in browser
```

## Style guide

### Markdown conventions

- Use **H1** (`#`) for page titles
- Use **H2** (`##`) for major sections
- Use **H3** (`###`) for subsections
- Use code blocks with language tags:
  ````markdown
  ```csharp
  var x = 42;
  ```
  ````

### Code examples

- Keep examples short and runnable
- Use real-world scenarios
- Always show error handling when relevant
- Include both ❌ (bad) and ✅ (good) patterns

### Cross-references

Use xref links for API references:

```markdown
See <xref:CloudStorageORM.Contexts.CloudStorageDbContext>
```

For internal doc links, use relative paths:

```markdown
[Configuration guide](configuration.md)
[Azure testing](testing-with-azurite.md)
```

### Emoji usage

Use sparingly for section headers to improve scannability:

- 📖 = guides and documentation
- ⚙️ = configuration
- 🔍 = queries and search
- 💾 = persistence and storage
- 🔄 = concurrency and synchronization
- 📦 = providers and packages
- 🚚 = migration
- 🎯 = samples and examples
- 🧪 = testing
- 🔧 = CI/CD and operations
- ⚠️ = warnings and troubleshooting

## Maintenance

### Keeping docs fresh

- Update docs when **public API changes**
- Update docs when **behavior changes**
- Review docs during **major version bumps**
- Keep examples in sync with **sample application**

### Adding new pages

1. Create `docs/my-new-topic.md`
2. Add entry to `docs/toc.yml`
3. Use cross-references from other pages
4. Test locally: `docfx docfx.json`
5. Submit in PR

### Deprecating pages

1. Mark content with **[DEPRECATED]**
2. Link to replacement guide
3. Keep page in toc but move to bottom or remove from nav after grace period
4. Remove from toc.yml once unreferenced

## Troubleshooting doc builds

### DocFX build fails

```bash
# Ensure template is installed
./scripts/setup-docfx-material.sh

# Clear cache
rm -rf api _site obj

# Rebuild
docfx docfx.json --force
```

### xref links broken

Check that you're using the correct namespace/type name:

```bash
# Search API files
grep -r "Your.Type.Name" api/
```

### Theme/styling not loading

```bash
# Regenerate from scratch
rm -rf _site api
./scripts/setup-docfx-material.sh
docfx docfx.json
```

## See also

- [DocFX official documentation](https://dotnet.github.io/docfx/)
- [docfx-material theme](https://github.com/ovasquez/docfx-material)
- [CloudStorageORM GitHub](https://github.com/rzavalik/CloudStorageORM)