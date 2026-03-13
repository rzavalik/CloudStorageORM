namespace SampleApp.DbContext
{
    using CloudStorageORM.DbContext;
    using Microsoft.EntityFrameworkCore;
    using Models;

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

            base.OnModelCreating(modelBuilder);
        }
    }

    public class MyAppDbContextCloudStorage : CloudStorageDbContext
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

            base.OnModelCreating(modelBuilder);
        }
    }
}
