namespace SampleApp
{
    using Microsoft.EntityFrameworkCore;
    using SampleApp.Models;
    using System;
    using System.Threading.Tasks;
    using SampleApp.DbContext;
    using CloudStorageORM.Options;
    using CloudStorageORM.Enums;
    using Microsoft.Extensions.DependencyInjection;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.StorageProviders;
    using CloudStorageORM.Extensions;

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
                Console.WriteLine($"🚀 Running using {storageType:G}...");

                Microsoft.EntityFrameworkCore.DbContext dbContext;
                var services = new ServiceCollection();

                // Register the IStorageProvider for CloudStorageORM
                if (storageType == StorageType.CloudStorageORM)
                {
                    // Register the IStorageProvider
                    var cloudStorageOptions = new CloudStorageOptions
                    {
                        Provider = CloudProvider.Azure,
                        ConnectionString = "UseDevelopmentStorage=true",
                        ContainerName = "sampleapp-container"
                    };

                    services.AddSingleton<CloudStorageOptions>(cloudStorageOptions);
                    services.AddSingleton<IStorageProvider, AzureBlobStorageProvider>();
                    services.AddEntityFrameworkCloudStorageORM(cloudStorageOptions);

                    // Register DbContext for CloudStorageORM
                    services.AddDbContext<MyAppDbContextCloudStorage>(options =>
                    {
                        // Pass CloudStorageOptions to configure the DbContext
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

                // For CloudStorageORM, initialize the container in the cloud
                if (storageType == StorageType.CloudStorageORM)
                {
                    var storageProvider = provider.GetRequiredService<IStorageProvider>();
                    await storageProvider.DeleteContainerAsync();
                    await storageProvider.CreateContainerIfNotExistsAsync();
                }

                // Create a scope and resolve the appropriate DbContext
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
                Console.WriteLine($"❌ An error occurred: {ex}");
            }
        }

        private static async Task RunSample(Microsoft.EntityFrameworkCore.DbContext context)
        {
            var repository = context.Set<User>();
            var userId = Guid.NewGuid().ToString();

            Console.WriteLine("➕ Creating a new user...");
            var newUser = new User
            {
                Id = userId,
                Name = "John Doe",
                Email = "john.doe@example.com"
            };
            context.Add(newUser);
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ User {newUser.Name} created.");

            Console.WriteLine("\n📃 Listing users...");
            var users = await repository.ToListAsync();
            foreach (var user in users)
            {
                Console.WriteLine($"- {user.Id}: {user.Name} ({user.Email})");
            }

            Console.WriteLine("\n✏️ Updating the user...");
            var usersList = await repository.ToListAsync();
            var userToUpdate = usersList.FirstOrDefault(u => u.Id == userId);
            if (userToUpdate != null)
            {
                userToUpdate.Name = "John Doe Updated";
                userToUpdate.Email = "john.doe.updated@example.com";
                context.Update(userToUpdate);
                await context.SaveChangesAsync();
                Console.WriteLine($"✅ User {userToUpdate.Name} updated.");
            }

            Console.WriteLine("\n🔎 Finding the updated user...");
            usersList = await repository.ToListAsync();
            var foundUser = usersList.FirstOrDefault(u => u.Id == userId);
            if (foundUser != null)
            {
                Console.WriteLine($"🎯 Found: {foundUser.Id} - {foundUser.Name} ({foundUser.Email})");
            }
            else
            {
                Console.WriteLine("❌ User not found.");
            }

            Console.WriteLine("\n🗑️ Deleting the user...");
            if (foundUser != null)
            {
                context.Remove(foundUser);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ User deleted.");
            }

            Console.WriteLine("\n📃 Listing users after deletion...");
            var usersAfterDelete = await repository.ToListAsync();
            foreach (var user in usersAfterDelete)
            {
                Console.WriteLine($"- {user.Id}: {user.Name} ({user.Email})");
            }

            if (usersAfterDelete.Count == 0)
            {
                Console.WriteLine("✅ No users found. Deletion confirmed.");
            }

            Console.WriteLine($"🏁 SampleApp Finished for {context.GetType().Name}.");
        }
    }
}
