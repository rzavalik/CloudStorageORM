
# 📦 CloudStorageORM - Library Documentation

**Target Framework**: `.NET 8`  
**Testing**: `xUnit`, `Shouldly`, `Moq`  
**Architecture**: `Builder Architecture`, `SOLID Principles`, `Clean Dependency Injection`

---

## Overview

The `CloudStorageORM` project is a lightweight, extensible, and testable Entity Framework Provider that allows using cloud object storage (e.g., Azure Blob Storage, Amazon S3, Google Cloud Storage) **as a database backend** for simple, scalable applications.

The library focuses on implementing CRUD operations using **cloud-native principles** such as:
- **Locking**, **Snapshot Isolation**
- **Blob / Object Storage** as backend
- **DbContext integration** for familiar development experience
- **Plug & Play** Storage Providers (Azure, AWS, GCP)

---

## Project Structure

| Folder/File | Purpose |
| :--- | :--- |
| `Abstractions/` | Core Interfaces for Providers and Internal Logic |
| `Infrastructure/` | EF Core Integration and Implementation Details |
| `Models/` | Entity and Metadata Definitions |
| `Services/` | Utility Services (Locking, Serialization) |
| `StorageProviders/` | Cloud Provider Implementations (e.g., AzureBlobStorageProvider) |
| `CloudStorageExtensions.cs` | Extension methods to inject CloudStorageORM into DI/EF |
| `CloudStorageOptions.cs` | Configuration options for Storage Provider |
| `CloudStorageSingletonOptionsInitializer.cs` | EF Core service initializer |
| `IStorageProvider.cs` | Interface for storage operations |
| `StorageProviderFactory.cs` | Future point for factory building multiple providers |

---

## Class-by-Class Documentation

### 📁 `Abstractions`

#### `IStorageProvider`
- **Purpose**: Defines CRUD operations over cloud storage (like Save, Get, Delete, List).
- **Intent**: Allow CloudStorageORM to remain agnostic of the underlying storage provider.
- **Key Methods**: 
  - `SaveAsync`
  - `GetAsync`
  - `DeleteAsync`
  - `ListAsync`
  - `ExistsAsync`

### 📁 `Infrastructure`

#### `CloudStorageExtensions`
- **Purpose**: Adds `.UseCloudStorageORM()` extension to `DbContextOptionsBuilder`.
- **Intent**: Seamless EF Core integration through Dependency Injection.

#### `CloudStorageOptions`
- **Purpose**: Encapsulates configuration settings.
- **Properties**:
  - `Provider` (Azure, AWS, GCP)
  - `ConnectionString`
  - `ContainerName`
  - `RootPath`
- **Intent**: Centralize storage configuration per context.

#### `CloudStorageSingletonOptionsInitializer`
- **Purpose**: Internal EF Core hook that ensures `CloudStorageORM` options are initialized once.
- **Intent**: Prevent misconfiguration at runtime.

### 📁 `Models`

#### `EntityMetadata`
- **Purpose**: Metadata representation (like ETag, LastModified, ContentType) for stored entities.
- **Intent**: Enable advanced scenarios (optimistic concurrency, audit, etc).

### 📁 `Services`

#### `BlobEntitySerializer`
- **Purpose**: Handles serialization/deserialization of entities into blobs (using JSON).
- **Intent**: Abstract serialization to allow future customizations (e.g., compressed, encrypted).

#### `BlobStorageLocker`
- **Purpose**: Provides optimistic concurrency and basic locking.
- **Intent**: Prevent concurrent writes, race conditions using cloud-native features (e.g., leases, etags).

### 📁 `StorageProviders`

#### `AzureBlobStorageProvider`
- **Purpose**: Implements `IStorageProvider` for **Azure Blob Storage**.
- **Intent**: Enable CRUD operations via Azure SDK.
- **Notes**: 
  - Uses `BlobContainerClient`
  - Uploads, downloads, deletes JSON blobs
  - Supports basic folder organization via "prefixes"

### 🔧 `StorageProviderFactory`
- **Purpose**: (Currently not implemented but envisioned)  
- **Intent**: Allow runtime provider selection based on `CloudProvider` enum (Azure, AWS, GCP).

---

## Design Patterns and Principles

- **Builder Pattern**: Configure via `.UseCloudStorageORM()`
- **Dependency Injection**: Injects `IStorageProvider` into the DbContext
- **SOLID**: Clear separation of concerns across Abstractions, Infrastructure, Services
- **Testability**: Every class can be tested using `Moq`, `Shouldly`, and `xUnit`
- **Extensibility**: Easy to add support for new providers (AWS S3, Google Storage)

---

## Current Status

✅ InMemory Provider for Testing  
✅ Azure Blob Storage Provider  
✅ Full CRUD Implementation  
✅ Integrated with EF Core  
✅ SampleApp showing usage  
🚧 AWS/GCP Providers not yet implemented  
🚧 Advanced Lock/Lease strategies to be evolved  

---

## Future Plans

- Implement `AWS S3 Provider`
- Implement `Google Cloud Storage Provider`
- Add `Blob Compression / Encryption`
- Extend `BlobEntitySerializer` to allow pluggable formats (JSON, Avro, ProtoBuf)
- Advanced Locking (distributed lock manager)
