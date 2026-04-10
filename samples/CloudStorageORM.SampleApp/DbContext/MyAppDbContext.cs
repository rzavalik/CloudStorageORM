using CloudStorageORM.Contexts;
using CloudStorageORM.Extensions;
using Microsoft.EntityFrameworkCore;
using SampleApp.Models;

namespace SampleApp.DbContext;

public class MyAppDbContextInMemory(DbContextOptions<MyAppDbContextInMemory> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<User>()
            .UseObjectETagConcurrency();

        base.OnModelCreating(modelBuilder);
    }
}

public class MyAppDbContextCloudStorage(DbContextOptions<MyAppDbContextCloudStorage> options)
    : CloudStorageDbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);

        base.OnModelCreating(modelBuilder);
    }
}