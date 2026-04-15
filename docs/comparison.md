# Comparison with Other Frameworks

CloudStorageORM provides a unique approach to cloud storage access in the .NET ecosystem. This page compares it with
similar solutions across different languages and platforms.

## What Makes CloudStorageORM Unique?

CloudStorageORM bridges the gap between **Object-Oriented Development** and **Cloud Storage** by providing an Entity
Framework-like experience. Most cloud storage solutions fall into one of two categories:

1. **Raw Client Libraries** - Direct API access (minimal abstraction)
2. **Abstraction Layers** - Unified interface across providers (but still file/blob-focused)

CloudStorageORM goes further by allowing you to map .NET classes directly to blob storage with automatic
serialization/deserialization, just like Entity Framework does for databases.

## Cross-Language Comparison

### Java: Hibernate + CData JDBC Driver

| Aspect                     | CloudStorageORM                                  | Hibernate + CData                    |
|----------------------------|--------------------------------------------------|--------------------------------------|
| **Learning Curve**         | Entity Framework developers feel at home         | Steeper - requires JDBC knowledge    |
| **Type Safety**            | Native .NET generics                             | Java generics via Hibernate entities |
| **Multi-Provider Support** | Azure Blob Storage, AWS S3 (Google Cloud coming) | Limited to JDBC-compatible sources   |
| **Serialization**          | JSON (built-in)                                  | CSV, JSON, XML (via CData)           |
| **Query Pattern**          | LINQ-style (IQueryable)                          | HQL/SQL translated to API calls      |
| **Native Integration**     | Designed for .NET 10+                            | Bridge layer overhead                |

**Verdict:** Similar philosophy, but CloudStorageORM is more tightly integrated with .NET conventions.

---

### Python: Cloud-Pathlib + Pydantic

| Aspect                   | CloudStorageORM                                   | Cloud-Pathlib + Pydantic                    |
|--------------------------|---------------------------------------------------|---------------------------------------------|
| **Unified API**          | Azure/AWS with single code path                   | Cloud-pathlib provides filesystem interface |
| **Type Mapping**         | Attributes-based configuration                    | Pydantic models for serialization           |
| **Ease of Use**          | DbContext + DbSet pattern (familiar to .NET devs) | Combine multiple libraries                  |
| **Async Support**        | First-class async patterns                        | Async through underlying libraries          |
| **Dependency Injection** | Built-in DI integration                           | Manual composition required                 |
| **DbSet Queries**        | Rich LINQ support                                 | Manual filtering/enumeration                |

**Verdict:** Python approach is more lightweight; CloudStorageORM is more opinionated and structured.

---

### Node.js/TypeScript: @itwin/object-storage

| Aspect                   | CloudStorageORM                            | @itwin/object-storage                          |
|--------------------------|--------------------------------------------|------------------------------------------------|
| **Provider Abstraction** | Multi-provider support with consistent API | Adapters for provider-specific implementations |
| **Object Mapping**       | Auto-serialization with attributes         | Manual mapping required                        |
| **Language Features**    | Leverages .NET attributes and reflection   | Leverages TypeScript interfaces                |
| **Enterprise Ready**     | Full transaction support                   | Basic transactional support                    |
| **Configuration**        | UseCloudStorageOrm() fluent API            | Adapter-based configuration                    |
| **Metadata Support**     | Integrated ETag and blob settings          | Provider-specific metadata handling            |

**Verdict:** @itwin/object-storage is more minimalist; CloudStorageORM provides more batteries-included features.

---

### Apache Libcloud (Multi-Language)

| Aspect            | CloudStorageORM                   | Apache Libcloud                  |
|-------------------|-----------------------------------|----------------------------------|
| **Goal**          | EF-like ORM for cloud storage     | Cloud provider abstraction layer |
| **Scope**         | Focused on object storage mapping | 50+ cloud provider types         |
| **Type Safety**   | Strong typing with .NET generics  | Dynamic/loosely typed            |
| **ORM Patterns**  | DbSet, LINQ, ETag support         | Key-value object interface       |
| **Serialization** | Automatic JSON mapping            | Manual serialization             |
| **Maturity**      | Actively maintained               | Long-term stability              |

**Verdict:** Libcloud is more about *abstraction*, CloudStorageORM is about *object mapping*.

---

### Rust: object_store

| Aspect                | CloudStorageORM                     | object_store                    |
|-----------------------|-------------------------------------|---------------------------------|
| **Abstraction Level** | High-level ORM patterns             | Low-level, zero-copy API        |
| **Performance**       | Optimized for .NET runtime          | Optimized for async Rust        |
| **Usage Pattern**     | DbContext/DbSet (application layer) | Direct API calls (library use)  |
| **Provider Support**  | Azure, AWS, Google Cloud (planned)  | AWS, Azure, Google Cloud, local |
| **Type Safety**       | .NET reflection and attributes      | Rust compile-time guarantees    |

**Verdict:** object_store is more performance-focused; CloudStorageORM is more developer-focused.

---

## Summary Table

| Language | Framework                | Type                | Closest to CloudStorageORM |
|----------|--------------------------|---------------------|----------------------------|
| **C#**   | **CloudStorageORM**      | **Full ORM**        | ⭐ This is it!              |
| Java     | Hibernate + CData GCS    | Relational Proxy    | ⚠️ Similar philosophy      |
| Python   | Cloud-Pathlib + Pydantic | Composition Pattern | ⚠️ Manual assembly         |
| Node.js  | @itwin/object-storage    | Adapter Pattern     | ⚠️ Less opinionated        |
| Go       | Apache Libcloud          | Abstraction Layer   | ❌ Different goal           |
| Rust     | object_store             | Low-level API       | ❌ Performance-focused      |

---

## When to Choose CloudStorageORM

Choose CloudStorageORM if you need:

✅ **Entity Framework-like experience** for cloud storage  
✅ **Strong typing** with automatic serialization  
✅ **Multi-provider support** (Azure Blob Storage, AWS S3, Google Cloud planned)  
✅ **LINQ query patterns** for enumerating objects  
✅ **Transaction support** across operations  
✅ **Native .NET integration** with dependency injection  
✅ **DbSet pattern** familiar to EF developers

---

## Alternatives to Consider

- **Raw Azure SDK / AWS SDK** - Maximum control, zero abstraction
- **Blob Abstraction Libraries** - Provider abstraction without ORM patterns
- **Document Databases** - If your data is semi-structured (CosmosDB, DynamoDB)
- **File Shares** - If POSIX-like filesystem semantics are acceptable (Azure Files)

---

## Contributing Feedback

If you've compared CloudStorageORM with another framework, please share your findings! Open an issue
on [GitHub](https://github.com/rzavalik/CloudStorageORM) with your comparison.