namespace SampleApp
{
    using Microsoft.EntityFrameworkCore;
    using SampleApp.Models;
    using SampleApp.Configuration;
    using CloudStorageORM.DbContext;
    using System;
    using System.Threading.Tasks;
    using SampleApp.DbContext;
    using Microsoft.Extensions.Options;
    using CloudStorageORM.StorageProviders;
    using CloudStorageORM.Options;

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

        private static IStorageConfigurationProvider GetConfiguration(StorageType storageType)
        {
            return storageType switch
            {
                StorageType.InMemory => new InMemoryStorageConfiguration("TestDatabase"),
                StorageType.CloudStorageORM => new CloudStorageOrmConfiguration(new CloudStorageORM.Options.CloudStorageOptions
                {
                    Provider = CloudStorageORM.Enums.CloudProvider.Azure,
                    ConnectionString = "UseDevelopmentStorage=true",
                    ContainerName = "sampleapp-container"
                }),
                _ => throw new NotSupportedException()
            };
        }

        private static async Task Execute(StorageType storageType)
        {
            try
            {
                Console.WriteLine($"🚀 Running using {storageType:G}...");

                if (storageType == StorageType.CloudStorageORM)
                {
                    var optionsBuilder = new DbContextOptionsBuilder<CloudStorageDbContext>();
                    var storageConfiguration = GetConfiguration(storageType);
                    storageConfiguration.Configure(optionsBuilder);
                    var cloudStorageOptions = new CloudStorageOptions
                    {
                        ConnectionString = "UseDevelopmentStorage=true",
                        ContainerName = "sampleapp-container"
                    };
                    var storageProvider = new AzureBlobStorageProvider(cloudStorageOptions);
                    using var context = new StorageDbContext(optionsBuilder.Options, cloudStorageOptions);
                    await RunSample(context);
                }
                else
                {
                    var optionsBuilder = new DbContextOptionsBuilder<InMemoryDbContext>();
                    var storageConfiguration = GetConfiguration(storageType);
                    storageConfiguration.Configure(optionsBuilder);
                    using var context = new InMemoryDbContext(optionsBuilder.Options);
                    await RunSample(context);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ An error occurred: {ex}");
            }
        }

        private static async Task RunSample<T>(T context)
            where T: Microsoft.EntityFrameworkCore.DbContext
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
            context.Add(newUser);
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ User {newUser.Name} created.");

            // 2. List
            Console.WriteLine("\n📃 Listing users...");
            var users = await repository.ToListAsync();
            foreach (var user in users)
            {
                Console.WriteLine($"- {user.Id}: {user.Name} ({user.Email})");
            }

            // 3. Update
            Console.WriteLine("\n✏️ Updating the user...");
            var usersList = (List<User>)await repository.ToListAsync();
            var userToUpdate = usersList.FirstOrDefault(u => u.Id == userId);
            if (userToUpdate != null)
            {
                userToUpdate.Name = "John Doe Updated";
                userToUpdate.Email = "john.doe.updated@example.com";
                context.Update(userToUpdate);
                await context.SaveChangesAsync();
                Console.WriteLine($"✅ User {userToUpdate.Name} updated.");
            }

            // 4. Find
            Console.WriteLine("\n🔎 Finding the updated user...");
            usersList = (List<User>)await repository.ToListAsync();
            var foundUser = usersList.FirstOrDefault(u => u.Id == userId);
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
            if (foundUser != null)
            {
                context.Remove(foundUser);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ User deleted.");
            }

            // 6. List again
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