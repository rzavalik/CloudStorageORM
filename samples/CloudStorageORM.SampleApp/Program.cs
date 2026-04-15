using System.Text;
using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using SampleApp.DbContext;
using SampleApp.Models;

namespace SampleApp;

/// <summary>
/// Console sample that demonstrates CloudStorageORM with InMemory, Azure, and AWS providers.
/// </summary>
public static class Program
{
    /// <summary>
    /// Shared HTTP client used by local endpoint reachability probes.
    /// </summary>
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(2) };

    /// <summary>
    /// Stable user identifier used to keep sample runs deterministic.
    /// </summary>
    private const string DeterministicUserId = "sample-user-001";

    /// <summary>
    /// Environment variable name for the default container or bucket.
    /// </summary>
    private const string ContainerNameEnvVar = "CLOUDSTORAGEORM_CONTAINER_NAME";

    /// <summary>
    /// Environment variable name for Azure connection string.
    /// </summary>
    private const string AzureConnectionStringEnvVar = "CLOUDSTORAGEORM_AZURE_CONNECTION_STRING";

    /// <summary>
    /// Environment variable name for AWS access key id.
    /// </summary>
    private const string AwsAccessKeyIdEnvVar = "CLOUDSTORAGEORM_AWS_ACCESS_KEY_ID";

    /// <summary>
    /// Environment variable name for AWS secret access key.
    /// </summary>
    private const string AwsSecretAccessKeyEnvVar = "CLOUDSTORAGEORM_AWS_SECRET_ACCESS_KEY";

    /// <summary>
    /// Environment variable name for AWS region.
    /// </summary>
    private const string AwsRegionEnvVar = "CLOUDSTORAGEORM_AWS_REGION";

    /// <summary>
    /// Environment variable name for AWS service URL.
    /// </summary>
    private const string AwsServiceUrlEnvVar = "CLOUDSTORAGEORM_AWS_SERVICE_URL";

    /// <summary>
    /// Environment variable name for AWS bucket override.
    /// </summary>
    private const string AwsBucketEnvVar = "CLOUDSTORAGEORM_AWS_BUCKET";

    /// <summary>
    /// Environment variable name that controls AWS ForcePathStyle.
    /// </summary>
    private const string AwsForcePathStyleEnvVar = "CLOUDSTORAGEORM_AWS_FORCE_PATH_STYLE";

    /// <summary>
    /// Storage backends exercised by this sample.
    /// </summary>
    private enum StorageType
    {
        /// <summary>
        /// EF Core InMemory provider.
        /// </summary>
        InMemory,

        /// <summary>
        /// CloudStorageORM with Azure Blob Storage.
        /// </summary>
        Azure,

        /// <summary>
        /// CloudStorageORM with AWS S3.
        /// </summary>
        Aws
    }

    /// <summary>
    /// Entry point that runs the sample against each supported storage type.
    /// </summary>
    /// <param name="args">Command-line arguments (currently unused).</param>
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("🚀 CloudStorageORM SampleApp Starting...");
        Console.WriteLine("");
        await Execute(StorageType.InMemory);
        Console.WriteLine("");
        await Execute(StorageType.Azure);
        Console.WriteLine("");
        await Execute(StorageType.Aws);
        Console.WriteLine("");
        Console.WriteLine("🏁 SampleApp Finished.");
    }

    /// <summary>
    /// Executes one end-to-end scenario for a selected storage type.
    /// </summary>
    /// <param name="storageType">Provider to run for this pass.</param>
    private static async Task Execute(StorageType storageType)
    {
        try
        {
            Console.WriteLine($"🚀 Running using EF {storageType:G} Provider...");
            Console.WriteLine("|");

            Microsoft.EntityFrameworkCore.DbContext dbContext;
            var services = new ServiceCollection();

            // Register the IStorageProvider for CloudStorageORM
            if (storageType is StorageType.Azure or StorageType.Aws)
            {
                var cloudProvider = storageType == StorageType.Azure ? CloudProvider.Azure : CloudProvider.Aws;
                var cloudStorageOptions = BuildCloudStorageOptionsFromEnvironment(cloudProvider);
                Console.WriteLine($"| ☁️ Cloud provider: {cloudStorageOptions.Provider}");

                var (isAvailable, reason) = await IsProviderAvailableAsync(cloudStorageOptions);
                if (!isAvailable)
                {
                    Console.WriteLine($"| ⚠️ Skipping {cloudStorageOptions.Provider} run: {reason}");
                    return;
                }

                // Register DbContext for CloudStorageORM
                services.AddDbContext<MyAppDbContextCloudStorage>(options =>
                {
                    // Pass CloudStorageOptions to configure the DbContext
                    options.UseCloudStorageOrm(opt =>
                    {
                        opt.Provider = cloudStorageOptions.Provider;
                        opt.ContainerName = cloudStorageOptions.ContainerName;
                        opt.Azure = cloudStorageOptions.Azure;
                        opt.Aws = cloudStorageOptions.Aws;
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
            if (storageType is StorageType.Azure or StorageType.Aws)
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

    /// <summary>
    /// Runs CRUD and transaction demonstrations using the provided DbContext.
    /// </summary>
    /// <param name="context">Configured DbContext instance.</param>
    private static async Task RunSample(Microsoft.EntityFrameworkCore.DbContext context)
    {
        var repository = context.Set<User>();

        Console.WriteLine("| 🧹 Clearing users before run...");
        var cleared = await repository.ClearAsync(context);
        Console.WriteLine($"| ✅ Cleared {cleared} existing users.");
        Console.WriteLine("|");

        var userId = DeterministicUserId;

        Console.WriteLine("| 📃 Listing users...");
        var users = await repository.ToListAsync();
        if (users.Count != 0)
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
        context.Add(newUser);
        await context.SaveChangesAsync();
        Console.WriteLine($"| ✅ User {newUser.Name} created (Id {newUser.Id}).");
        var createdUser = context is MyAppDbContextCloudStorage
            ? await ReadStoredUserAsync(context, userId)
            : newUser;
        Console.WriteLine(
            $"| 🏷️ Created user payload: {createdUser?.Id} - {createdUser?.Name} ({createdUser?.Email}) | ETag: {createdUser?.ETag ?? "<null>"}");
        Console.WriteLine("|");

        Console.WriteLine("| 📃 Listing users...");
        users = await repository.ToListAsync();
        foreach (var user in users)
        {
            Console.WriteLine($"|   {user.Id}: {user.Name} ({user.Email})" + (newUser.Id == user.Id ? " ✅" : ""));
        }

        Console.WriteLine("|");
        Console.WriteLine("| ✏️ Updating the user...");
        var userToUpdate = await FindUserByIdAsync(context, repository, userId);
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
        var foundUser = await FindUserByIdAsync(context, repository, userId);
        Console.WriteLine(foundUser != null
            ? $"| 🎯 Found: {foundUser.Id} - {foundUser.Name} ({foundUser.Email})"
            : "| ❌ User not found.");

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

        if (context is MyAppDbContextCloudStorage)
        {
            await RunTransactionScenario(context, repository);
        }
        else
        {
            Console.WriteLine("|");
            Console.WriteLine("| ↩️ Skipping transaction scenario for InMemory provider.");
        }

        Console.WriteLine("|");
        Console.WriteLine($"🏁 SampleApp Finished for {context.GetType().Name}.");
    }

    /// <summary>
    /// Demonstrates rollback and commit semantics for CloudStorageORM transactions.
    /// </summary>
    /// <param name="context">DbContext used to execute transaction operations.</param>
    /// <param name="repository">User DbSet used for verification reads.</param>
    private static async Task RunTransactionScenario(Microsoft.EntityFrameworkCore.DbContext context,
        DbSet<User> repository)
    {
        const string rollbackUserId = $"{DeterministicUserId}-tx-rollback";
        const string commitUserId = $"{DeterministicUserId}-tx-commit";

        Console.WriteLine("|");
        Console.WriteLine("| 🔁 Transaction scenario: rollback should not persist...");
        await using (var rollbackTx = await context.Database.BeginTransactionAsync())
        {
            context.Add(new User
            {
                Id = rollbackUserId,
                Name = "Tx Rollback",
                Email = "tx.rollback@example.com"
            });

            await context.SaveChangesAsync();
            await rollbackTx.RollbackAsync();
        }

        context.ChangeTracker.Clear();
        var rollbackFound = await FindUserByIdAsync(context, repository, rollbackUserId);
        Console.WriteLine(rollbackFound is null
            ? "| ✅ Rollback verification passed: user was not persisted."
            : "| ❌ Rollback verification failed: user is still present.");

        Console.WriteLine("|");
        Console.WriteLine("| ✅ Transaction scenario: commit should persist...");
        await using (var commitTx = await context.Database.BeginTransactionAsync())
        {
            context.Add(new User
            {
                Id = commitUserId,
                Name = "Tx Commit",
                Email = "tx.commit@example.com"
            });

            await context.SaveChangesAsync();
            await commitTx.CommitAsync();
        }

        context.ChangeTracker.Clear();
        var commitFound = await FindUserByIdAsync(context, repository, commitUserId);
        Console.WriteLine(commitFound is not null
            ? "| ✅ Commit verification passed: user was persisted."
            : "| ❌ Commit verification failed: user was not found.");
    }

    /// <summary>
    /// Checks whether a configured provider is reachable before running the sample.
    /// </summary>
    /// <param name="options">Provider options used for endpoint probing.</param>
    /// <returns>A tuple indicating availability and a human-readable reason.</returns>
    private static async Task<(bool IsAvailable, string Reason)> IsProviderAvailableAsync(CloudStorageOptions options)
    {
        return options.Provider switch
        {
            CloudProvider.Azure => await IsAzureAvailableAsync(options),
            CloudProvider.Aws => await IsAwsAvailableAsync(options),
            _ => (false, $"Provider {options.Provider} not supported by SampleApp.")
        };
    }

    /// <summary>
    /// Probes local Azurite availability when using emulator-style Azure configuration.
    /// </summary>
    /// <param name="options">Cloud storage options containing Azure settings.</param>
    /// <returns>A tuple indicating availability and probe details.</returns>
    private static async Task<(bool IsAvailable, string Reason)> IsAzureAvailableAsync(CloudStorageOptions options)
    {
        if (!string.Equals(options.Azure.ConnectionString, "UseDevelopmentStorage=true",
                StringComparison.OrdinalIgnoreCase))
        {
            return (true, "Azure connection string is not using local emulator; skipping local probe.");
        }

        var isReachable = await IsHttpEndpointReachableAsync("http://127.0.0.1:10000");
        return isReachable
            ? (true, "Azurite is reachable.")
            : (false, "Azurite is not reachable at http://127.0.0.1:10000.");
    }

    /// <summary>
    /// Probes configured AWS endpoint availability (typically LocalStack in local runs).
    /// </summary>
    /// <param name="options">Cloud storage options containing AWS settings.</param>
    /// <returns>A tuple indicating availability and probe details.</returns>
    private static async Task<(bool IsAvailable, string Reason)> IsAwsAvailableAsync(CloudStorageOptions options)
    {
        var endpoint = options.Aws.ServiceUrl;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return (true, "AWS service URL not configured; skipping local probe.");
        }

        var isReachable = await IsHttpEndpointReachableAsync(endpoint);
        return isReachable
            ? (true, "AWS endpoint is reachable.")
            : (false, $"AWS endpoint is not reachable at {endpoint}.");
    }

    /// <summary>
    /// Performs a lightweight HTTP GET to determine whether an endpoint is reachable.
    /// </summary>
    /// <param name="endpoint">Endpoint URL to probe.</param>
    /// <returns><see langword="true" /> when the request can be issued; otherwise <see langword="false" />.</returns>
    private static async Task<bool> IsHttpEndpointReachableAsync(string endpoint)
    {
        try
        {
            using var response = await HttpClient.GetAsync(endpoint);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Finds a user by id using provider-specific fast paths when available.
    /// </summary>
    /// <param name="context">Current DbContext.</param>
    /// <param name="repository">User set used for fallback query paths.</param>
    /// <param name="userId">User identifier to search for.</param>
    /// <returns>The matching user, or <see langword="null" /> when not found.</returns>
    private static async Task<User?> FindUserByIdAsync(
        Microsoft.EntityFrameworkCore.DbContext context,
        DbSet<User> repository,
        string userId)
    {
        if (context is MyAppDbContextCloudStorage)
        {
            var database = context.GetService<Microsoft.EntityFrameworkCore.Storage.IDatabase>();
            if (database is CloudStorageDatabase cloudStorageDatabase)
            {
                return await cloudStorageDatabase.TryLoadByPrimaryKeyAsync<User>(userId, context);
            }

            var users = await repository.ToListAsync();
            return users.FirstOrDefault(u => u.Id == userId);
        }

        return repository.FirstOrDefault(u => u.Id == userId);
    }

    /// <summary>
    /// Reads a user directly from object storage with metadata to surface the latest ETag.
    /// </summary>
    /// <param name="context">Current CloudStorageORM context.</param>
    /// <param name="userId">User identifier used to build the storage path.</param>
    /// <returns>The stored user payload, or <see langword="null" /> when not found.</returns>
    private static async Task<User?> ReadStoredUserAsync(Microsoft.EntityFrameworkCore.DbContext context, string userId)
    {
        var storageProvider = context.GetService<IStorageProvider>();
        var pathResolver = new BlobPathResolver(storageProvider);
        var path = pathResolver.GetPath(typeof(User), userId);
        var stored = await storageProvider.ReadWithMetadataAsync<User>(path);
        stored.Value?.ETag = stored.ETag;

        return stored.Value;
    }

    /// <summary>
    /// Builds provider options from environment variables with sample-friendly defaults.
    /// </summary>
    /// <param name="provider">Cloud provider to configure.</param>
    /// <returns>A populated <see cref="CloudStorageOptions" /> instance.</returns>
    private static CloudStorageOptions BuildCloudStorageOptionsFromEnvironment(CloudProvider provider)
    {
        const string defaultContainerName = "sampleapp-container";
        var containerName = provider == CloudProvider.Aws
            ? Environment.GetEnvironmentVariable(AwsBucketEnvVar)
              ?? Environment.GetEnvironmentVariable(ContainerNameEnvVar)
              ?? defaultContainerName
            : Environment.GetEnvironmentVariable(ContainerNameEnvVar) ?? defaultContainerName;

        return new CloudStorageOptions
        {
            Provider = provider,
            ContainerName = containerName,
            Azure = new CloudStorageAzureOptions
            {
                ConnectionString = Environment.GetEnvironmentVariable(AzureConnectionStringEnvVar)
                                   ?? "UseDevelopmentStorage=true"
            },
            Aws = new CloudStorageAwsOptions
            {
                AccessKeyId = Environment.GetEnvironmentVariable(AwsAccessKeyIdEnvVar) ?? "test",
                SecretAccessKey = Environment.GetEnvironmentVariable(AwsSecretAccessKeyEnvVar) ?? "test",
                Region = Environment.GetEnvironmentVariable(AwsRegionEnvVar) ?? "us-east-1",
                ServiceUrl = Environment.GetEnvironmentVariable(AwsServiceUrlEnvVar) ?? "http://localhost:4566",
                ForcePathStyle = ParseBool(Environment.GetEnvironmentVariable(AwsForcePathStyleEnvVar), true)
            }
        };
    }


    /// <summary>
    /// Parses a boolean value with a fallback default.
    /// </summary>
    /// <param name="value">Raw input string.</param>
    /// <param name="defaultValue">Value returned when parsing fails.</param>
    /// <returns>The parsed boolean value or <paramref name="defaultValue" />.</returns>
    private static bool ParseBool(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed)
            ? parsed
            : defaultValue;
    }
}