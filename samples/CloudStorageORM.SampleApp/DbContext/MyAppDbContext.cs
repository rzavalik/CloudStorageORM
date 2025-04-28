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
        }

        public DbSet<User> Users { get; set; }
    }
}
