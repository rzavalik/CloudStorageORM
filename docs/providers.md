# Provider guides

## Azure Blob Storage

CloudStorageORM's **Azure Blob Storage** provider is production-ready and fully featured.

### Connection options

#### Development with Azurite

```csharp
storage.Provider = CloudProvider.Azure;
storage.ContainerName = "local-container";
storage.Azure.ConnectionString = "UseDevelopmentStorage=true";
```

Then start Azurite:

```bash
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  mcr.microsoft.com/azure-storage/azurite:latest \
  azurite --blobHost 0.0.0.0 --skipApiVersionCheck
```

#### Production on Azure

```csharp
storage.Azure.ConnectionString = 
    "DefaultEndpointsProtocol=https;" +
    "AccountName=myaccount;" +
    "AccountKey=mykey;" +
    "EndpointSuffix=core.windows.net";
```

### Features

- ✅ Full CRUD operations
- ✅ LINQ queries with range predicates
- ✅ Transactions with durability
- ✅ ETag optimistic concurrency
- ✅ Validation and error handling

### Limitations

- Not implemented: blob leases for distributed locking (planned v1.1.0)
- Container is created automatically on first use when missing

## AWS S3

CloudStorageORM's **AWS S3** provider is production-ready and fully featured.

### Connection options

#### Development with LocalStack

```csharp
storage.Provider = CloudProvider.Aws;
storage.ContainerName = "local-bucket";
storage.Aws.AccessKeyId = "test";
storage.Aws.SecretAccessKey = "test";
storage.Aws.Region = "us-east-1";
storage.Aws.ServiceUrl = "http://127.0.0.1:4566";
storage.Aws.ForcePathStyle = true;
```

Then start LocalStack:

```bash
docker run -d -p 4566:4566 \
  -e SERVICES=s3 \
  localstack/localstack:3
```

#### Production on AWS

```csharp
storage.Provider = CloudProvider.Aws;
storage.ContainerName = "my-production-bucket";
storage.Aws.AccessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
storage.Aws.SecretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
storage.Aws.Region = "us-west-2";
// ServiceUrl is not needed for production AWS
```

### Features

- ✅ Full CRUD operations
- ✅ LINQ queries with range predicates
- ✅ Transactions with durability
- ✅ ETag optimistic concurrency
- ✅ Validation and error handling

### Limitations

- Not implemented: S3 object locks for distributed locking (planned v1.1.0)
- Bucket is created automatically on first use when missing

## Google Cloud Storage

**Not yet implemented.** Google Cloud Storage support is planned for v1.3.0+.

See the [Roadmap](../ROADMAP.md) for details.

## Multi-provider applications

To support multiple providers in the same application, use environment-based configuration:

```csharp
var provider = Environment.GetEnvironmentVariable("CLOUD_PROVIDER");

options.UseCloudStorageOrm(storage =>
{
    storage.ContainerName = Environment.GetEnvironmentVariable("CONTAINER_NAME");

    if (provider == "Azure")
    {
        storage.Provider = CloudProvider.Azure;
        storage.Azure.ConnectionString = Environment.GetEnvironmentVariable("AZURE_CONNECTION_STRING");
    }
    else if (provider == "Aws")
    {
        storage.Provider = CloudProvider.Aws;
        storage.Aws.AccessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        storage.Aws.SecretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        storage.Aws.Region = Environment.GetEnvironmentVariable("AWS_REGION");
    }
});
```

## See also

- [Configuration](configuration.md)
- [Testing with Azurite](testing-with-azurite.md)
- [Testing with LocalStack](testing-with-localstack.md)