namespace SampleApp
{
    using CloudStorageORM.DbContext;
    using CloudStorageORM.Enums;
    using CloudStorageORM.Options;
    using CloudStorageORM.StorageProviders;
    using System;
    using System.Threading.Tasks;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("🚀 CloudStorageORM SampleApp Starting...");

            var options = new CloudStorageOptions
            {
                Provider = CloudProvider.Azure,
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "sampleapp-container"
            };

            var storageProvider = new AzureBlobStorageProvider(options);
            var context = new CloudStorageDbContext(options, storageProvider);

            try
            {
                var repository = context.Set<User>();
                var userId = Guid.NewGuid().ToString();

                // 1. Create
                Console.WriteLine("➕ Creating a new user...");
                var newUser = new User
                {
                    Id = userId,
                    Name = "John Doe",
                    Email = "john.doe@example.com"
                };
                await repository.AddAsync(userId, newUser);
                Console.WriteLine($"✅ User {newUser.Name} created.");

                // 2. List
                Console.WriteLine("\n📃 Listing users...");
                var users = await repository.ListAsync();
                foreach (var user in users)
                {
                    Console.WriteLine($"- {user.Id}: {user.Name} ({user.Email})");
                }

                // 3. Update
                Console.WriteLine("\n✏️ Updating the user...");
                newUser.Name = "John Doe Updated";
                newUser.Email = "john.doe.updated@example.com";
                await repository.UpdateAsync(userId, newUser);
                Console.WriteLine($"✅ User {newUser.Name} updated.");

                // 4. Find
                Console.WriteLine("\n🔎 Finding the updated user...");
                var foundUser = await repository.FindAsync(userId);
                if (foundUser != null)
                {
                    Console.WriteLine($"🎯 Found: {foundUser.Id} - {foundUser.Name} ({foundUser.Email})");
                }
                else
                {
                    Console.WriteLine("❌ User not found.");
                }

                // 5. Delete
                Console.WriteLine("\n🗑️ Deleting the user...");
                await repository.RemoveAsync(userId);
                Console.WriteLine($"✅ User deleted.");

                // 6. List again
                Console.WriteLine("\n📃 Listing users after deletion...");
                var usersAfterDelete = await repository.ListAsync();
                foreach (var user in usersAfterDelete)
                {
                    Console.WriteLine($"- {user.Id}: {user.Name} ({user.Email})");
                }

                if (usersAfterDelete.Count == 0)
                {
                    Console.WriteLine("✅ No users found. Deletion confirmed.");
                }
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
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
