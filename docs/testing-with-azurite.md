# 🧪 Testing CloudStorageORM with Azurite

CloudStorageORM uses [Azurite](https://github.com/Azure/Azurite) to simulate Azure Blob Storage locally during testing.

This allows you to run tests without a real Azure subscription, fully offline and free.

---

## 🚀 Running Azurite Locally

You can run Azurite easily using Docker:

```bash
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 --name azurite mcr.microsoft.com/azure-storage/azurite
```

## 🔗 Connection String for Testing
The tests use the following connection string:

```
UseDevelopmentStorage=true
```
This automatically points to your local Azurite instance.

## 🛑 Common Issues

### Azurite not running

Make sure the Docker container is running:
```bash
docker ps
```

### Port conflicts
Ensure ports 10000–10002 are not used by other processes.
