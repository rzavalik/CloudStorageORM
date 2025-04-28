# CloudStorageORM.SampleApp

Welcome to the official sample app for **CloudStorageORM**!

This project demonstrates how to use the **CloudStorageORM** library to persist and query data using **Azure Blob Storage** as the data source, through an **Entity Framework Core (EF Core)**-like experience.

The SampleApp runs two examples side-by-side:
- One using **InMemory** database.
- One using **CloudStorageORM** with Azure Blob Storage.

---

## 🔍 What does this SampleApp do?

For both storage types (**InMemory** and **CloudStorageORM**):

1. **Creates a User**
2. **Lists Users**
3. **Updates the User**
4. **Finds the updated User**
5. **Deletes the User**
6. **Confirms that no Users are left**

Everything is fully logged in the Console output, showing the behavior of the persistence operations.

---

## 💡 How the App is Organized

The `Program.cs` file drives the execution. It uses two DbContext configurations:

- `MyAppDbContextInMemory`: EF Core using InMemoryDatabase.
- `MyAppDbContextCloudStorage`: EF Core using CloudStorageORM with Azure Blob Storage.

Each DbContext implements the same operations to show how CloudStorageORM can mimic a traditional database using object storage.


---

## 🎓 Technologies Demonstrated

| Feature | Demonstrated |
|:---|:---|
| EF Core Concepts | DbContext, DbSet, LINQ Queries |
| InMemory Database | For comparison |
| CloudStorageORM | Using Blob Storage like a database |
| CRUD Operations | Create, Read, Update, Delete |
| Azure Blob Storage | For persistent storage |
| Asynchronous programming | `await` / `async` all over the sample |
| IQueryable and IAsyncEnumerable | Correct handling inside ORM |

---

## 🛍️ How to Run Locally

1. Clone the Repository:
   ```bash
   git clone https://github.com/rzavalik/CloudStorageORM.git
   cd CloudStorageORM/samples/CloudStorageORM.SampleApp
   ```

2. Configure your Azure Storage:
   - The app currently uses the **Azurite** emulator by default.
   - If you want to use real Azure Blob Storage, update the connection string and container name in `Program.cs`.

3. Run the App:
   ```bash
   dotnet run
   ```

4. See the output in the console:
   - First part: InMemory database operations.
   - Second part: CloudStorageORM with Azure Blob Storage.

---

## 📖 Example Console Output

```plaintext
🚀 Running using InMemory...
📃 Listing users...
- 72112332-dae0-4110-ae93-3afcb3d135da: John Doe (john.doe@example.com)
...
🚀 Running using CloudStorageORM...
📃 Listing users...
- 72112332-dae0-4110-ae93-3afcb3d135da: John Doe (john.doe@example.com)
...
🏁 SampleApp Finished.
```

---

## 🔄 What this Sample Demonstrates About CloudStorageORM

- **Seamless replacement** for traditional EF Core DbContext.
- **LINQ support** over Blob Storage.
- **Asynchronous and Synchronous queries**.
- **Simulated DbSet behavior** with Blob persistence.
- **Minimal code changes** between InMemory and Cloud Storage modes.

---

## 📘 Additional Notes

- When running with CloudStorageORM, each entity is persisted individually as a **Blob object** inside your container.
- This sample is useful for:
  - Testing CloudStorageORM capabilities.
  - Understanding how object storage can be used like a database for small/medium applications.
  - Comparing InMemory vs Cloud persistence side-by-side.

---

## 🌐 Links

- [Main Repository](https://github.com/rzavalik/CloudStorageORM)
- [CloudStorageORM NuGet Package](#)
- [Documentation](#)

---

## 💡 Future Improvements

- Implement `SaveChangesAsync` for real update/persist/delete operations in Blob.
- Add E2E API sample using ASP.NET Core.
- Add Azure Storage configuration options.

---

> Made with ❤️ by [Rodrigo Zavalik](https://github.com/rzavalik) and contributors!

---