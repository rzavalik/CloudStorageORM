using CloudStorageORM.Contexts;
using CloudStorageORM.Extensions;
using Microsoft.EntityFrameworkCore;
using SampleApp.Models;

namespace SampleApp.DbContext;

/// <summary>
/// InMemory DbContext used to show baseline EF behavior without object storage.
/// </summary>
/// <param name="options">Typed context options configured by dependency injection.</param>
public class MyAppDbContextInMemory(DbContextOptions<MyAppDbContextInMemory> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    /// <summary>
    /// Users set used by the sample CRUD flow.
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Configures model metadata for the in-memory sample context.
    /// </summary>
    /// <param name="modelBuilder">Model builder used to configure entities.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<User>()
            .UseObjectETagConcurrency();

        base.OnModelCreating(modelBuilder);
    }
}

/// <summary>
/// CloudStorageORM DbContext used to demonstrate object-storage-backed behavior.
/// </summary>
/// <param name="options">Typed context options configured by dependency injection.</param>
public class MyAppDbContextCloudStorage(DbContextOptions<MyAppDbContextCloudStorage> options)
    : CloudStorageDbContext(options)
{
    /// <summary>
    /// Users set used by the sample CRUD flow.
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Configures model metadata for the cloud-storage sample context.
    /// </summary>
    /// <param name="modelBuilder">Model builder used to configure entities.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);

        base.OnModelCreating(modelBuilder);
    }
}