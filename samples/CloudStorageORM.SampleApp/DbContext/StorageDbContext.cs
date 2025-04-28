namespace SampleApp.DbContext
{
    using Microsoft.EntityFrameworkCore;
    using SampleApp.Models;

    public class StorageDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        public StorageDbContext(DbContextOptions<StorageDbContext> options)
            : base(options)
        {
        }
    }
}