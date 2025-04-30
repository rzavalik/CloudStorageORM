namespace SampleApp.DbContext
{
    using CloudStorageORM.Options;
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

            base.OnModelCreating(modelBuilder);
        }
    }

    public class MyAppDbContextCloudStorage : DbContext
    {
        private readonly CloudStorageOptions _cloudStorageOptions;

        public MyAppDbContextCloudStorage(
            DbContextOptions<MyAppDbContextCloudStorage> options, 
            CloudStorageOptions cloudStorageOptions)
           : base(options)
        {
            _cloudStorageOptions = cloudStorageOptions;

            var optionsExtensions = options.Extensions.ToList();
            foreach (var ext in optionsExtensions)
            {
                Console.WriteLine($"➡️ Extension registered: {ext.GetType().FullName}");
            }
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
