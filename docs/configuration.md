# Configuration

CloudStorageORM uses a fluent configuration model through `CloudStorageOptions` to configure storage providers and
behavior.

## Basic configuration

```csharp
services.AddDbContext<AppDbContext>(options =>
{
    options.UseCloudStorageOrm(storage =>
    {
        // Common settings
        storage.Provider = CloudProvider.Azure;
        storage.ContainerName = "my-container";

        // Provider-specific settings
        storage.Azure.ConnectionString = "DefaultEndpointsProtocol=https;...";
    });
});
```

## Azure configuration

Configure CloudStorageORM to use **Azure Blob Storage**:

```csharp
storage.Provider = CloudProvider.Azure;
storage.ContainerName = "my-blob-container";
storage.Azure.ConnectionString = "DefaultEndpointsProtocol=https;AccountName=...";
```

### Connection string formats

- **Development** (Azurite):  
  `UseDevelopmentStorage=true`

- **Production** (Azure):  
  `DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=...;EndpointSuffix=core.windows.net`

- **Connection string from environment**:
  ```csharp
  var connStr = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
  storage.Azure.ConnectionString = connStr;
  ```

## AWS configuration

Configure CloudStorageORM to use **AWS S3**:

```csharp
storage.Provider = CloudProvider.Aws;
storage.ContainerName = "my-s3-bucket";
storage.Aws.AccessKeyId = "AKIA...";
storage.Aws.SecretAccessKey = "...";
storage.Aws.Region = "us-east-1";
storage.Aws.ServiceUrl = "http://127.0.0.1:4566"; // For LocalStack
storage.Aws.ForcePathStyle = false; // Set to true for S3-compatible services
```

### Environment-based AWS config

```csharp
storage.Provider = CloudProvider.Aws;
storage.ContainerName = Environment.GetEnvironmentVariable("AWS_BUCKET") ?? "my-bucket";
storage.Aws.AccessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
storage.Aws.SecretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
storage.Aws.Region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
storage.Aws.ServiceUrl = Environment.GetEnvironmentVariable("AWS_SERVICE_URL");
```

## Configuration validation

CloudStorageORM validates the configuration at context initialization. Common errors:

| Error                        | Cause                  | Fix                                              |
|------------------------------|------------------------|--------------------------------------------------|
| `CloudProvider.NotSupported` | Invalid provider       | Use `CloudProvider.Azure` or `CloudProvider.Aws` |
| `ContainerName` required     | No container specified | Set `storage.ContainerName`                      |
| Connection failed            | Invalid credentials    | Verify connection string or AWS keys             |

## Observability configuration (optional)

CloudStorageORM can emit structured logs, tracing spans, and diagnostics hooks. All are enabled by default and can be
controlled under `CloudStorageOptions.Observability`.

Current behavior on `main`:

- `EnableLogging` controls CloudStorageORM `ILogger` events.
- `EnableTracing` controls CloudStorageORM `ActivitySource` span creation.
- `EnableDiagnostics` is part of options/debug info; custom runtime `DiagnosticListener` events are not emitted yet.

```csharp
storage.Observability.EnableLogging = true;
storage.Observability.EnableTracing = true;
storage.Observability.EnableDiagnostics = true;

// Optional custom names (reserved for future custom-source wiring)
storage.Observability.ActivitySourceName = "CloudStorageORM";
storage.Observability.DiagnosticListenerName = "CloudStorageORM";
```

If your application does not consume these signals, you can disable them individually.

See [Observability guide](observability.md) for practical usage profiles and consumer examples.

## Model configuration

### Primary keys

Entities must define an explicit primary key:

```csharp
modelBuilder.Entity<User>().HasKey(x => x.Id);
```

### Optional: ETag concurrency

Enable optimistic concurrency checking (see [Concurrency guide](concurrency.md)):

```csharp
modelBuilder.Entity<User>().UseObjectETagConcurrency();
```

## See also

- [Getting started](getting-started.md)
- [Transactions](transactions.md)
- [Concurrency](concurrency.md)
- [API reference](api-reference.md)