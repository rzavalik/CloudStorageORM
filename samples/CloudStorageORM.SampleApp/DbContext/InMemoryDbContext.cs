namespace SampleApp.DbContext
{
    using Microsoft.EntityFrameworkCore;
    using SampleApp.Models;

    public class InMemoryDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        public InMemoryDbContext(DbContextOptions<InMemoryDbContext> options)
            : base(options)
        {
        }
    }
}