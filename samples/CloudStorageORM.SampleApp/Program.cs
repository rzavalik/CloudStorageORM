namespace SampleApp
{
    using System;
    using System.Threading.Tasks;
    using CloudStorageORM.Enums;
    using CloudStorageORM.Extensions;
    using CloudStorageORM.Options;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using SampleApp.DbContext;
    using SampleApp.Models;

    public class Program
    {
        public enum StorageType
        {
            InMemory,
            CloudStorageORM
        }

        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("🚀 CloudStorageORM SampleApp Starting...");
            Console.WriteLine("");
            await Execute(StorageType.InMemory);
            Console.WriteLine("");
            await Execute(StorageType.CloudStorageORM);
            Console.WriteLine("");
            Console.WriteLine($"🏁 SampleApp Finished.");
        }

        private static async Task Execute(StorageType storageType)
        {
            try
            {
                Console.WriteLine($"🚀 Running using EF {storageType:G} Provider...");
                Console.WriteLine("|");

                Microsoft.EntityFrameworkCore.DbContext dbContext;
                var services = new ServiceCollection();

                // Register the IStorageProvider for CloudStorageORM
                if (storageType == StorageType.CloudStorageORM)
                {
                    var cloudStorageOptions = new CloudStorageOptions
                    {
                        Provider = CloudProvider.Azure,
                        ConnectionString = "UseDevelopmentStorage=true",
                        ContainerName = "sampleapp-container"
                    };

                    // ✅ Registra os serviços do EF e do CloudStorageORM
                    services.AddEntityFrameworkCloudStorageORM(cloudStorageOptions);

                    // ✅ Registra o DbContext e injeta as opções
                    services.AddDbContext<MyAppDbContextCloudStorage>((serviceProvider, options) =>
                    {
                        options.UseInternalServiceProvider(serviceProvider);
                        options.UseCloudStorageORM(opt =>
                        {
                            opt.Provider = cloudStorageOptions.Provider;
                            opt.ConnectionString = cloudStorageOptions.ConnectionString;
                            opt.ContainerName = cloudStorageOptions.ContainerName;
                        });
                    });
                }
                else
                {
                    // Register DbContext for InMemory with correct options
                    services.AddDbContext<MyAppDbContextInMemory>(options =>
                    {
                        // Use In-Memory for this DbContext
                        options.UseInMemoryDatabase("InMemoryDb");
                    });
                }

                var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                if (storageType == StorageType.CloudStorageORM)
                {
                    dbContext = scope.ServiceProvider.GetRequiredService<MyAppDbContextCloudStorage>();
                }
                else
                {
                    dbContext = scope.ServiceProvider.GetRequiredService<MyAppDbContextInMemory>();
                }

                await RunSample(dbContext);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ An error occurred: {ex}");
            }
        }

        private static async Task RunSample(Microsoft.EntityFrameworkCore.DbContext context)
        {
            var repository = context.Set<User>();
            var userId = Guid.NewGuid().ToString();

            Console.WriteLine("| 📃 Listing users...");
            var users = await repository.ToListAsync();
            if ((users?.Any() ?? false))
            {
                foreach (var user in users)
                {
                    Console.WriteLine($"|   {user.Id}: {user.Name} ({user.Email})");
                }
            }
            else
            {
                Console.WriteLine("| ❌ No users found.");
            }

            Console.WriteLine("|");
            Console.WriteLine("| ➕ Creating a new user...");
            var newUser = new User
            {
                Id = userId,
                Name = "John Doe",
                Email = "john.doe@example.com"
            };
            await context.AddAsync(newUser);
            await context.SaveChangesAsync();
            Console.WriteLine($"| ✅ User {newUser.Name} created (Id {newUser.Id}).");
            Console.WriteLine("|");

            Console.WriteLine("| 📃 Listing users...");
            users = await repository.ToListAsync();
            foreach (var user in users)
            {
                Console.WriteLine($"|   {user.Id}: {user.Name} ({user.Email})" + (newUser.Id == user.Id ? " ✅" : ""));
            }

            Console.WriteLine("|");
            Console.WriteLine("| ✏️ Updating the user...");
            var userToUpdate = await repository.FindAsync(userId);
            if (userToUpdate != null)
            {
                userToUpdate.Name = "John Doe Updated";
                userToUpdate.Email = "john.doe.updated@example.com";
                context.Update(userToUpdate);
                await context.SaveChangesAsync();
                Console.WriteLine($"| ✅ User {userToUpdate.Name} updated (Id {userToUpdate.Id}).");
            }

            Console.WriteLine("|");
            Console.WriteLine("| 🔎 Finding the updated user...");
            var foundUser = await repository.FindAsync(userId);
            if (foundUser != null)
            {
                Console.WriteLine($"| 🎯 Found: {foundUser.Id} - {foundUser.Name} ({foundUser.Email})");
            }
            else
            {
                Console.WriteLine("| ❌ User not found.");
            }

            Console.WriteLine("|");
            Console.WriteLine("| 🗑️ Deleting the user...");
            if (foundUser != null)
            {
                context.Remove(foundUser);
                await context.SaveChangesAsync();
                Console.WriteLine($"| ✅ User deleted (Id {foundUser.Id}).");
            }

            Console.WriteLine("|");
            Console.WriteLine("| 📃 Listing users after deletion...");
            var usersAfterDelete = await repository.ToListAsync();
            foreach (var user in usersAfterDelete)
            {
                Console.WriteLine($"|   {user.Id}: {user.Name} ({user.Email})" + (newUser.Id == user.Id ? " ✅" : ""));
            }

            if (usersAfterDelete.Count == 0)
            {
                Console.WriteLine("| ✅ No users found. Deletion confirmed.");
            }

            Console.WriteLine("|");
            Console.WriteLine($"🏁 SampleApp Finished for {context.GetType().Name}.");
        }
    }
}
