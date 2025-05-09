namespace SampleApp.DbContext
{
    using CloudStorageORM.Enums;
    using CloudStorageORM.Extensions;
    using Microsoft.EntityFrameworkCore;
    using SampleApp.Models;

    public class MyAppDbContextInMemory : DbContext
    {
        public MyAppDbContextInMemory(
            DbContextOptions<MyAppDbContextInMemory> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);
        }
    }

    public class MyAppDbContextCloudStorage : DbContext
    {
        public MyAppDbContextCloudStorage(
            DbContextOptions<MyAppDbContextCloudStorage> options)
           : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            modelBuilder
                .ApplyBlobSettingsConventions()
                .Validate(CloudProvider.Azure);
        }
    }
}
