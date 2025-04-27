# CloudStorageORM

**Simplify persistence. Embrace scalability. Build the future.**

CloudStorageORM is a lightweight and powerful library that enables developers to use cloud storage (Azure Blob Storage, AWS S3, Google Cloud Storage) as a reliable and scalable data source through Entity Framework.  
Built with .NET 8, following Clean Architecture and SOLID principles, it empowers small and medium applications to focus on business logic while reducing the complexity of managing traditional databases.

![License](https://img.shields.io/badge/license-GPLv3-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![Build Status](https://github.com/rzavalik/CloudStorageORM/actions/workflows/ci.yml/badge.svg)
![NuGet](https://img.shields.io/nuget/v/CloudStorageORM?color=blue)
[![Contributing](https://img.shields.io/badge/Contributing-Guidelines-blue.svg)](./CONTRIBUTING.md)
[![Security Policy](https://img.shields.io/badge/Security-Policy-blue.svg)](./SECURITY.md)

[See the full Roadmap](./ROADMAP.md)

---

## ✨ Features

- ☁️ Use Azure Blob Storage, AWS S3, or Google Cloud Storage as your database
- 🛠️ Builder and Clean Architecture ready
- 🔥 Full Unit Test coverage
- 🔒 Optimized for scalability, reliability, and concurrency
- 🎯 Targeting .NET 8 and Entity Framework integration
- 📦 Installable via NuGet

---

## 📦 Installation

```bash
dotnet add package CloudStorageORM
```

Or search for `CloudStorageORM` in the NuGet Package Manager.

---

## 🚀 Getting Started

Configure your storage and start using Entity Framework to interact with cloud objects as if they were entities.

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

👉 Full examples and documentation coming soon!

---

## 🛡️ License

This project is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.  
Commercial use without prior authorization is not allowed.  
Please read the [LICENSE](LICENSE) file for more details.

---

## 🤝 Contributing

Contributions are welcome!  
If you have ideas for improvements, bug fixes, or new features, feel free to open an issue or submit a pull request.

Please make sure to follow our [Pull Request Guidelines](./.github/PULL_REQUEST_TEMPLATE.md).

Thank you for helping improve CloudStorageORM! 🚀

---

> _"CloudStorageORM empowers developers to move faster, scale smarter, and build stronger applications by leveraging the true power of cloud storage."_ 🚀
