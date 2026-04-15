# CloudStorageORM - Roadmap

This document outlines the evolution of CloudStorageORM from the current release line into the next planned milestones.

---

## ✅ Current released version

### v1.0.13

- Version bump to `1.0.13` for the current release line
- Documentation refreshed to keep local test guidance aligned with CI emulator setup
- Server-side `Skip`/`Take` pushdown for supported query shapes
- Observability improvements and refreshed guidance for logging, tracing, and diagnostics options
- ETag-based optimistic concurrency support to CloudStorageORM:
  - Introduces StorageObject<T> to encapsulate the stored value, ETag, and existence state, and adds IETag for entities that optionally expose an ETag property.
  - The storage pipeline now supports SaveAsync and DeleteAsync operations with ETag conditions, and both Azure Blob Storage and AWS S3 providers have been updated to enforce concurrency checks.
  - Model configuration has also been updated so entities can be configured for ETag concurrency, and tests were added to cover the new concurrency scenarios.


---

## 📋 Previous releases

### v1.0.12

- Version bump to `1.0.12` for the previous release line
- Documentation refreshed to keep local test guidance aligned with CI emulator setup
- ETag-based optimistic concurrency support to CloudStorageORM:
  - Introduces StorageObject<T> to encapsulate the stored value, ETag, and existence state, and adds IETag for entities that optionally expose an ETag property.
  - The storage pipeline now supports SaveAsync and DeleteAsync operations with ETag conditions, and both Azure Blob Storage and AWS S3 providers have been updated to enforce concurrency checks.
  - Model configuration has also been updated so entities can be configured for ETag concurrency, and tests were added to cover the new concurrency scenarios.


### v1.0.11

- Targets `.NET 10`
- Azure Blob Storage provider implemented
- AWS S3 provider implemented
- EF-style configuration through `UseCloudStorageOrm(...)`
- `CloudStorageDbContext` integration for consumer contexts
- Primary-key range query operators: `>`, `>=`, `<`, `<=` with direct range-aware loading
- Enhanced GitHub Actions workflows with Node.js 24 opt-in
- Dual NuGet package publishing to NuGet.org and GitHub Packages
- SBOM (Software Bill of Materials) export in CI pipeline
- One-type-per-file library structure enforced
- Sample app runs the same CRUD flow against EF InMemory, Azure, and AWS
- LINQ query execution optimized for range predicates
- Unit and integration tests with full coverage
- Integration coverage includes Azurite and LocalStack paths
- Coverage collection/reporting is wired with Coverlet + ReportGenerator
- File-scoped namespace style enforced repository-wide
- Keep roadmap, CI workflows, and package metadata aligned with the current release line
- Preserve sample app parity between EF InMemory and CloudStorageORM providers
- Maintain HTML coverage reporting workflow and contributor guidance


### v1.0.10

- Targets `.NET 10`
- Azure Blob Storage provider implemented
- AWS S3 provider implemented
- EF-style configuration through `UseCloudStorageOrm(...)`
- `CloudStorageDbContext` integration for consumer contexts
- Sample app runs the same CRUD flow against EF InMemory, Azure, and AWS
- LINQ query execution improved to better match expected EF provider behavior
- Unit and integration tests are in place
- Integration coverage includes Azurite and LocalStack paths
- Coverage collection/reporting is wired with Coverlet + ReportGenerator
- File-scoped namespace style enforced repository-wide

---

## 🔜 Planned future milestones

### v1.1.0

- Add provider-native lock and unlock support (Azure lease-based coordination)
- Add AWS concurrency coordination via conditional/object-lock style mechanisms
- Improve concurrency control mechanisms across provider implementations
- Extend Azure provider for richer blob lifecycle semantics
- Expand sample scenarios to include concurrency-sensitive operations

---

### v1.2.0

- AWS provider hardening (resilience, retries, and diagnostics)
- Expanded AWS integration test matrix (lifecycle, edge cases, and failure scenarios)
- CI improvements for AWS test execution reliability
- Performance profiling for larger AWS object sets

---

### v1.3.0

- Google Cloud Storage provider
- Save, read, delete, and list operations on Google Cloud Storage
- Improve runtime provider selection behavior
- Extend configuration options for multi-provider scenarios

---

### v1.4.0

- Snapshot and versioning support
- Point-in-time entity recovery concepts
- Provider-specific capabilities matrix for Azure, AWS, and Google

---

### v1.5.0

- Production-readiness guidance per provider
- Hardening and performance review of query/persistence flows
- Additional samples beyond the console application
- Broader end-to-end validation scenarios

---

## 🌟 Future ideas

- Cross-provider replication (Azure ↔ AWS ↔ Google)
- Event-driven change tracking (webhooks, queues, or storage events)
- Encryption-at-rest and encryption-in-transit options
- Automatic retry policies and transient fault handling
- Advanced metadata indexing for faster queries

---

> CloudStorageORM is evolving toward a practical EF-style experience over object storage, starting with Azure and expanding provider support over time.
