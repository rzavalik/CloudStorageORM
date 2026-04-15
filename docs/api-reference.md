# API reference

The complete auto-generated API documentation for CloudStorageORM, extracted from source code and XML documentation.

## Main namespaces

### Core contexts

- <xref:CloudStorageORM.Contexts> — Base `CloudStorageDbContext` for consumer applications

### Configuration and setup

- <xref:CloudStorageORM.Extensions> — Configuration extensions (`UseCloudStorageOrm`, etc.)
- <xref:CloudStorageORM.Options> — Configuration model (`CloudStorageOptions`, Azure/AWS options)

### Providers

- <xref:CloudStorageORM.Providers> — Provider factory and base abstractions
- <xref:CloudStorageORM.Providers.Azure.StorageProviders> — Azure Blob Storage provider
- <xref:CloudStorageORM.Providers.Aws.StorageProviders> — AWS S3 provider

### Querying and repositories

- <xref:CloudStorageORM.Repositories> — Repository helpers and queryable implementations
- <xref:CloudStorageORM.Infrastructure> — Query pipeline and EF integration

### Model support

- <xref:CloudStorageORM.Abstractions> — Base interfaces and attributes (including `IETag`)
- <xref:CloudStorageORM.Enums> — Enums like `CloudProvider`

## By common task

### I want to...

**...configure CloudStorageORM**

- See <xref:CloudStorageORM.Extensions.CloudStorageOrmExtensions>
- See <xref:CloudStorageORM.Options.CloudStorageOptions>

**...use transactions**

- See `CloudStorageDbContext.Database.BeginTransactionAsync()`
- See transaction classes in <xref:CloudStorageORM.Infrastructure>

**...implement optimistic concurrency**

- See <xref:CloudStorageORM.Extensions.ModelBuilderExtensions>
- See <xref:CloudStorageORM.Abstractions.IETag>

**...write custom queries**

- See <xref:CloudStorageORM.Repositories.CloudStorageRepository%601>
- See <xref:CloudStorageORM.Infrastructure.CloudStorageQueryable%601>

**...clear a set efficiently**

- See <xref:CloudStorageORM.Extensions.CloudStorageDbSetExtensions>
- Use `ClearAsync(this DbSet<TEntity> dbSet, DbContext context, CancellationToken cancellationToken = default)`

**...build a custom provider**

- See <xref:CloudStorageORM.Interfaces.StorageProviders.IStorageProvider>
- See <xref:CloudStorageORM.Providers.ProviderFactory>

## See also

- [Getting started](getting-started.md)
- [Configuration guide](configuration.md)
- [Query patterns](query-patterns.md)
- [Library guide](CloudStorageORM.md)