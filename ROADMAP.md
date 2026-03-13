# CloudStorageORM - Roadmap

This document outlines the evolution of CloudStorageORM from the current release line into the next planned milestones.

---

## ✅ Current released version

### v1.0.8

- Targets `.NET 10`
- Azure Blob Storage provider implemented
- EF-style configuration through `UseCloudStorageOrm(...)`
- `CloudStorageDbContext` integration for consumer contexts
- Sample app runs the same CRUD flow against EF InMemory and CloudStorageORM
- LINQ query execution improved to better match expected EF provider behavior
- Unit and integration tests are in place
- Coverage collection/reporting is wired with Coverlet + ReportGenerator
- File-scoped namespace style enforced repository-wide
- Finalize documentation alignment for `.NET 10`
- Keep roadmap, CI workflows, and package metadata aligned with the current release line
- Preserve sample app parity between EF InMemory and CloudStorageORM
- Harden query execution and regression coverage around provider behavior
- Maintain HTML coverage reporting workflow and contributor guidance

---

## 🔜 Planned future milestones

### v1.1.0

- Add lock and unlock support in storage providers
- Improve concurrency control mechanisms
- Extend Azure provider for richer blob lifecycle semantics
- Expand sample scenarios to include concurrency-sensitive operations

---

### v1.2.0

- AWS S3 storage provider
- Save, read, delete, and list operations on AWS
- Integrate AWS provider into the existing abstraction layer
- Add sample configuration path for AWS support

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
