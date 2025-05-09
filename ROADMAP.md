# CloudStorageORM - Roadmap

This document outlines the planned evolution of CloudStorageORM over time.

---

## 🚀 Version Plan

### v1.0.7 (Current - In Progress)
- Azure Blob Storage provider (basic CRUD support)
- Core abstractions (IStorageProvider, Repository, Options)
- Unit tests for Azure provider and repository
- Minimal sample app demonstrating Azure CRUD operations

---

### v1.1.0 (Next)
- Add Lock and Unlock support in Storage Providers
- Implement concurrency control mechanisms
- Extend Azure provider to support lease-based locking
- Update sample app to demonstrate locking scenarios

---

### v1.2.0
- AWS S3 Storage Provider
- Implement Save, Read, Delete, List operations on AWS
- Integrate AWS provider into existing abstraction layer
- Expand sample app to support AWS via configuration

---

### v1.3.0
- Google Cloud Storage Provider
- Implement Save, Read, Delete, List operations on Google Cloud
- Support multiple provider selection at runtime
- Extend configuration options

---

### v1.4.0
- Implement Snapshot and Versioning support
- Allow creating point-in-time snapshots of entities
- Integrate snapshotting with Azure, AWS, and Google (where supported)

---

### v1.5 (First Major Release)
- Complete CRUD, Lock, and Snapshot features for Azure, AWS, and Google
- Finalize production-quality documentation
- Publish stable NuGet package
- Add advanced sample applications (Web API + Console)

---

## 🌟 Future Ideas
- Cross-provider replication (Azure ↔ AWS ↔ Google)
- Event-driven change tracking (webhooks or queues)
- Encryption-at-rest and encryption-in-transit options
- Automatic retry policies and transient fault handling
- Advanced metadata indexing for faster queries

---

> 🚀 CloudStorageORM aims to make building scalable, cloud-native applications simpler, faster, and more reliable.
