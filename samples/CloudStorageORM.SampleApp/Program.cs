using CloudStorageORM;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudStorageORM.SampleApp
{
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
                // Saving a new User
                var newUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "John Doe",
                    Email = "john.doe@example.com"
                };

                Console.WriteLine("Saving a new user...");
                await context.Set<User>().AddAsync(newUser);
                await context.SaveChangesAsync();
                Console.WriteLine($"✅ User {newUser.Name} saved.");

                // Listing all users
                Console.WriteLine("Listing all users...");
                var users = await context.Set<User>().ToListAsync();

                foreach (var user in users)
                {
                    Console.WriteLine($"- {user.Id}: {user.Name} ({user.Email})");
                }

                // Reading a single user
                Console.WriteLine("Reading a specific user...");
                var singleUser = await context.Set<User>().FindAsync(newUser.Id);

                if (singleUser != null)
                {
                    Console.WriteLine($"🎯 Found user: {singleUser.Name}");
                }
                else
                {
                    Console.WriteLine("❌ User not found.");
                }

                // Deleting the user
                Console.WriteLine("Deleting the user...");
                context.Set<User>().Remove(singleUser);
                await context.SaveChangesAsync();
                Console.WriteLine($"✅ User {singleUser.Name} deleted.");

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
