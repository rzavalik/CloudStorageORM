# CloudStorageORM

**Simplify persistence. Embrace scalability. Build the future.**

CloudStorageORM is a lightweight and powerful library that enables developers to use cloud storage (Azure Blob Storage, AWS S3, Google Cloud Storage) as a reliable and scalable data source through Entity Framework.  
Built with .NET 8, following Clean Architecture and SOLID principles, it empowers small and medium applications to focus on business logic while reducing the complexity of managing traditional databases.

![License](https://img.shields.io/badge/license-GPLv3-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![NuGet](https://img.shields.io/nuget/v/CloudStorageORM?color=blue)
![Build Status](https://github.com/rzavalik/CloudStorageORM/actions/workflows/ci.yml/badge.svg)
[![Contributing](https://img.shields.io/badge/Contributing-Guidelines-blue.svg)](./CONTRIBUTING.md)
[![Security Policy](https://img.shields.io/badge/Security-Policy-blue.svg)](./SECURITY.md)

[👉 See the full Roadmap](./ROADMAP.md)

---

## ✨ Features

- ☁️ Use Azure Blob Storage, AWS S3, or Google Cloud Storage as your database
- 🛠️ Builder Pattern and Clean Architecture ready
- 🔥 Full Unit Test coverage using xUnit, Shouldly, and Moq
- 🔒 Optimized for scalability, reliability, and concurrency control
- 🛆 Available on NuGet for easy installation
- 🎯 Built with .NET 8 and Entity Framework integration in mind

---

## 🛆 Installation

Install via CLI:

```bash
dotnet add package CloudStorageORM --version 0.1.0-beta
```

Or search for `CloudStorageORM` in the NuGet Package Manager inside Visual Studio.

---

## 🚀 Getting Started

Start by configuring your cloud storage provider:

```csharp
var options = new CloudStorageOptions
{
    Provider = CloudProvider.Azure,
    ConnectionString = "<your-connection-string>",
    ContainerName = "your-container"
};

var context = new CloudStorageDbContext(options);
var users = await context.Set<User>().ToListAsync();
```

![SampleApp](https://github.com/user-attachments/assets/4184b418-23bf-4371-a636-7cef41b8f1f9)

> 📚 Full examples and extended documentation are coming soon!

---

## 🧪 Running Tests Locally

CloudStorageORM uses [Azurite](https://github.com/Azure/Azurite) to simulate Azure Blob Storage locally for unit testing.  
See [Testing with Azurite](./docs/testing-with-azurite.md) to configure your local environment.

---

## 🛡️ License

This project is licensed under the **GNU General Public License v3.0 (GPL-3.0-only)**.  
Commercial use without prior authorization is not allowed.  
See the [LICENSE](./LICENSE) file for more information.

---

## 🤝 Contributing

We welcome contributions from the community! 🚀  
If you'd like to help, please read our [Contributing Guidelines](./CONTRIBUTING.md) and [Pull Request Template](./.github/PULL_REQUEST_TEMPLATE.md).

Thank you for helping make CloudStorageORM even better!

---

> _"CloudStorageORM empowers developers to move faster, scale smarter, and build stronger applications by leveraging the true power of cloud storage."_ 🚀

