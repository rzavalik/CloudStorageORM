namespace SampleApp
{
    using CloudStorageORM.DbContext;
    using CloudStorageORM.Enums;
    using CloudStorageORM.Options;
    using System;
    using System.Threading.Tasks;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 CloudStorageORM SampleApp Starting...");

            var options = new CloudStorageOptions
            {
                Provider = CloudProvider.Azure,
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "sampleapp-container"
            };

            var context = new CloudStorageDbContext(options);

            try
            {
                var newUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "John Doe",
                    Email = "john.doe@example.com"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ An error occurred: {ex.Message}");
            }

            Console.WriteLine("🏁 SampleApp Finished.");
        }
    }

    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
}
