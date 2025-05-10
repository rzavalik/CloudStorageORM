namespace CloudStorageORM.IntegrationTests.Azure.DbContext
{
    using CloudStorageORM.DbContext;
    using CloudStorageORM.IntegrationTests.Azure.Models;
    using Microsoft.EntityFrameworkCore;

    public class IntegrationTestDbContext : CloudStorageDbContext
    {
        public IntegrationTestDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
