# CloudStorageORM Documentation

An Entity Framework-style ORM provider for .NET that persists entities into cloud object storage.

## 🚀 Get started in 5 minutes

[**Quick start guide →**](getting-started.md)

Spin up a working example with Azure Blob Storage or AWS S3 in just a few lines of C#.

## 📚 Documentation sections

### For developers

- [**Getting started**](getting-started.md) — Setup, first entity, basic operations
- [**Configuration**](configuration.md) — Connect to Azure or AWS, tune options
- [**Query patterns**](query-patterns.md) — LINQ queries, filtering, range queries
- [**Transactions**](transactions.md) — Durable transactions with recovery
- [**Concurrency**](concurrency.md) — Optimistic locking with ETags
- [**Provider guides**](providers.md) — Azure Blob Storage and AWS S3 details

### For contributors & operators

- [**Library documentation**](CloudStorageORM.md) — Architecture, public APIs, project structure
- [**Sample application**](sampleapp.md) — End-to-end CRUD example
- [**Testing with Azurite**](testing-with-azurite.md) — Local Azure testing
- [**Testing with LocalStack**](testing-with-localstack.md) — Local AWS testing
- [**CI workflow**](ci.md) — Build, test, and deployment pipeline
- [**API reference**](api-reference.md) — Auto-generated API docs

## ✅ Current status

| Aspect                   | Status              | Details                              |
|--------------------------|---------------------|--------------------------------------|
| **Azure Blob Storage**   | ✅ Production-ready  | Full CRUD, transactions, concurrency |
| **AWS S3**               | ✅ Production-ready  | Full CRUD, transactions, concurrency |
| **Google Cloud Storage** | 🚧 Planned (v1.3.0) | Coming soon                          |
| **.NET version**         | ✅ .NET 10           | `net10.0` target                     |
| **Package**              | ✅ NuGet             | `CloudStorageORM`                    |

## 🔗 Quick links

- 📦 [NuGet package](https://www.nuget.org/packages/CloudStorageORM)
- 🐙 [GitHub repository](https://github.com/rzavalik/CloudStorageORM)
- 📋 [Roadmap](../ROADMAP.md)
- 🛡️ [Security policy](../SECURITY.md)

## 🤝 Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines and development setup.

## 📜 License

GNU General Public License v3.0 (GPL-3.0-or-later). See [LICENSE](../LICENSE) for details.