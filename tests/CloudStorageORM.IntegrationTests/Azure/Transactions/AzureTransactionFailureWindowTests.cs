using System.ComponentModel.DataAnnotations;
using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Options;
using CloudStorageORM.Providers.Azure.StorageProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shouldly;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace CloudStorageORM.IntegrationTests.Azure.Azure.Transactions;

public class AzureTransactionFailureWindowTests(StorageFixture fixture) : IClassFixture<StorageFixture>
{
    [Fact]
    public async Task RollbackWindow_SaveChangesThenRollback_DoesNotPersistEntity()
    {
        fixture.EnsureAvailableOrSkip();

        var (provider, containerName) = await CreateIsolatedProviderAsync("rollback");
        await using var context = CreateContext(containerName);

        var entity = new AzureTxEntity { Id = $"rb-{Guid.NewGuid():N}", Name = "rollback" };
        context.Add(entity);

        await using var tx = await context.Database.BeginTransactionAsync();
        await context.SaveChangesAsync();
        await tx.RollbackAsync();

        var path = new BlobPathResolver(provider).GetPath(typeof(AzureTxEntity), entity.Id);
        var loaded = await provider.ReadAsync<AzureTxEntity>(path);
        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task CommitWindow_SaveChangesThenCommit_PersistsEntity()
    {
        fixture.EnsureAvailableOrSkip();

        var (provider, containerName) = await CreateIsolatedProviderAsync("commit");
        await using var context = CreateContext(containerName);

        var entity = new AzureTxEntity { Id = $"cm-{Guid.NewGuid():N}", Name = "commit" };
        context.Add(entity);

        await using var tx = await context.Database.BeginTransactionAsync();
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        var path = new BlobPathResolver(provider).GetPath(typeof(AzureTxEntity), entity.Id);
        var loaded = await provider.ReadAsync<AzureTxEntity>(path);
        loaded.ShouldNotBeNull();
        loaded.Id.ShouldBe(entity.Id);
    }

    [Fact]
    public async Task RecoveryWindow_CommittedManifest_ReplaysAndCompletes()
    {
        fixture.EnsureAvailableOrSkip();

        var (provider, _) = await CreateIsolatedProviderAsync("recovery");
        var txId = Guid.NewGuid();
        var entityId = $"rc-{Guid.NewGuid():N}";
        var entityPath = new BlobPathResolver(provider).GetPath(typeof(AzureTxEntity), entityId);
        var manifestPath = $"__cloudstorageorm/tx/{txId:D}/manifest.json";

        await provider.SaveAsync(manifestPath, new RecoveryManifest
        {
            TransactionId = txId,
            State = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CommittedAtUtc = DateTimeOffset.UtcNow,
            AppliedOperationCount = 0,
            Operations =
            [
                new RecoveryOperation
                {
                    Sequence = 0,
                    Kind = 0,
                    Path = entityPath,
                    PayloadJson = $"{{\"id\":\"{entityId}\",\"name\":\"Recovered\"}}"
                }
            ]
        });

        var manager = new CloudStorageTransactionManager(provider);
        await manager.BeginTransactionAsync();
        await manager.RollbackTransactionAsync();

        var loaded = await provider.ReadAsync<AzureTxEntity>(entityPath);
        loaded.ShouldNotBeNull();
        loaded.Name.ShouldBe("Recovered");

        var replayedManifest = await provider.ReadAsync<RecoveryManifest>(manifestPath);
        replayedManifest.State.ShouldBe(2);
        replayedManifest.AppliedOperationCount.ShouldBe(1);
    }

    [Fact]
    public async Task CrossWriterConflictWindow_CommitAfterExternalUpdate_ThrowsConcurrencyException()
    {
        fixture.EnsureAvailableOrSkip();

        var (provider, containerName) = await CreateIsolatedProviderAsync("conflict");
        var id = $"cf-{Guid.NewGuid():N}";
        var path = new BlobPathResolver(provider).GetPath(typeof(AzureTxEntity), id);

        await provider.SaveAsync(path, new AzureTxEntity { Id = id, Name = "v1" });
        var original = await provider.ReadWithMetadataAsync<AzureTxEntity>(path);

        await using var context = CreateContext(containerName);
        var tracked = new AzureTxEntity { Id = id, Name = "tx-update", ETag = original.ETag };
        context.Attach(tracked);
        context.Entry(tracked).Property(x => x.ETag).OriginalValue = original.ETag;
        context.Entry(tracked).State = EntityState.Modified;

        await using var tx = await context.Database.BeginTransactionAsync();
        await context.SaveChangesAsync();

        await provider.SaveAsync(path, new AzureTxEntity { Id = id, Name = "v2" });

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => tx.CommitAsync());
    }

    [Fact]
    public async Task CrossWriterConflictWindow_EmitsTransactionAndConcurrencyEvents()
    {
        fixture.EnsureAvailableOrSkip();

        var logCollector = new EventIdLogCollector();
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddProvider(new EventIdLoggerProvider(logCollector));
        });

        var (provider, containerName) = await CreateIsolatedProviderAsync("obs");
        var id = $"obs-{Guid.NewGuid():N}";
        var path = new BlobPathResolver(provider).GetPath(typeof(AzureTxEntity), id);

        await provider.SaveAsync(path, new AzureTxEntity { Id = id, Name = "v1" });
        var original = await provider.ReadWithMetadataAsync<AzureTxEntity>(path);

        await using var context = CreateContext(containerName, loggerFactory);
        var tracked = new AzureTxEntity { Id = id, Name = "tx-update", ETag = original.ETag };
        context.Attach(tracked);
        context.Entry(tracked).Property(x => x.ETag).OriginalValue = original.ETag;
        context.Entry(tracked).State = EntityState.Modified;

        await using var tx = await context.Database.BeginTransactionAsync();
        await context.SaveChangesAsync();
        await provider.SaveAsync(path, new AzureTxEntity { Id = id, Name = "v2" });

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => tx.CommitAsync());

        logCollector.EventIds.ShouldContain(Observability.CloudStorageOrmEventIds.TransactionBeginning);
        logCollector.EventIds.ShouldContain(Observability.CloudStorageOrmEventIds.ConcurrencyConflict);
    }

    private AzureTxDbContext CreateContext(string containerName, ILoggerFactory? loggerFactory = null)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AzureTxDbContext>()
            .UseCloudStorageOrm(cfg =>
            {
                cfg.Provider = CloudProvider.Azure;
                cfg.ContainerName = containerName;
                cfg.Azure = new CloudStorageAzureOptions
                {
                    ConnectionString = fixture.ConnectionString
                };
            });

        if (loggerFactory is not null)
        {
            optionsBuilder.UseLoggerFactory(loggerFactory);
        }

        var options = optionsBuilder.Options;

        return new AzureTxDbContext(options);
    }

    private async Task<(AzureBlobStorageProvider Provider, string ContainerName)> CreateIsolatedProviderAsync(
        string scenario)
    {
        var containerName = $"tx-{scenario}-{Guid.NewGuid():N}"[..30];
        var provider = new AzureBlobStorageProvider(fixture.ConnectionString, containerName);
        await provider.CreateContainerIfNotExistsAsync();
        return (provider, containerName);
    }

    private sealed class AzureTxDbContext(DbContextOptions<AzureTxDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AzureTxEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<AzureTxEntity>().UseObjectETagConcurrency();
        }
    }

    private sealed class AzureTxEntity
    {
        [MaxLength(100)] public string Id { get; init; } = string.Empty;

        [MaxLength(100)] public string Name { get; init; } = string.Empty;

        [MaxLength(256)] public string? ETag { get; init; }
    }

    private sealed class RecoveryManifest
    {
        public Guid TransactionId { get; set; }
        public int State { get; init; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset? CommittedAtUtc { get; set; }
        public DateTimeOffset? CompletedAtUtc { get; set; }
        public List<RecoveryOperation> Operations { get; set; } = [];
        public int AppliedOperationCount { get; init; }
    }

    private sealed class RecoveryOperation
    {
        public int Sequence { get; set; }
        public int Kind { get; set; }
        public string Path { get; set; } = string.Empty;
        public string? PayloadJson { get; set; }
        public string? IfMatchETag { get; set; }
    }

    private sealed class EventIdLogCollector
    {
        public List<EventId> EventIds { get; } = [];
    }

    private sealed class EventIdLoggerProvider(EventIdLogCollector collector) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new EventIdLogger(collector);

        public void Dispose()
        {
        }
    }

    private sealed class EventIdLogger(EventIdLogCollector collector) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            collector.EventIds.Add(eventId);
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}