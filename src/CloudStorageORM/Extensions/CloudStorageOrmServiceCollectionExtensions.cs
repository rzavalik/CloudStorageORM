namespace CloudStorageORM.Extensions
{
    using CloudStorageORM.DbContext;
    using CloudStorageORM.Enums;
    using CloudStorageORM.Infrastructure;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using CloudStorageORM.StorageProviders;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public static class CloudStorageOrmServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkCloudStorageORM(
            this IServiceCollection services, 
            CloudStorageOptions storageOptions)
        {
            services.AddSingleton<IStorageProvider>(provider =>
            {
                var options = provider.GetRequiredService<CloudStorageOptions>();
                return new AzureBlobStorageProvider(options);
            });
            services.AddDbContext<Microsoft.EntityFrameworkCore.DbContext>((serviceProvider, options) =>
            {
                var storageProvider = serviceProvider.GetRequiredService<IStorageProvider>();
                options.UseCloudStorageORM(builder =>
                {
                    builder.Provider = CloudProvider.Azure;
                    builder.ConnectionString = "UseDevelopmentStorage=true";
                    builder.ContainerName = "sample-container";
                });
            });
            services.AddSingleton(storageOptions);

            // Register the necessary services for CloudStorageORM
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseProvider, CloudStorageDatabaseProvider>());
            services.TryAddSingleton<IDbContextServices, CloudStorageDbContextServices>();
            services.TryAddSingleton<IDbContextTransactionManager, CloudStorageTransactionManager>();
            services.TryAddSingleton<IDatabaseCreator, CloudStorageDatabaseCreator>();
            services.TryAddSingleton<IModelSource, ModelSource>();
            services.TryAddSingleton<IModelRuntimeInitializer, ModelRuntimeInitializer>();
            services.TryAddSingleton<IDbSetInitializer, CloudStorageDbSetInitializer>();
            services.TryAddSingleton<ISingletonOptionsInitializer, SingletonOptionsInitializer>();

            return services;
        }
    }
}
