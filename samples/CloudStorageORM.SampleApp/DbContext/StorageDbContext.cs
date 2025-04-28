namespace SampleApp.DbContext
{
    using CloudStorageORM.Interfaces.StorageProviders;
    using Microsoft.EntityFrameworkCore;
    using SampleApp.Models;

    public class StorageDbContext : DbContext
    {
        private readonly IStorageProvider _storageProvider;

        public StorageDbContext(
            DbContextOptions<StorageDbContext> options, 
            IStorageProvider storageProvider)
            : base(options)
        {
            _storageProvider = storageProvider;
        }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Add any additional model configurations if needed
        }
    }
}
