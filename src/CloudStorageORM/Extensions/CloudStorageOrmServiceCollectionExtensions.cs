namespace CloudStorageORM.Extensions
{
    using CloudStorageORM.DbContext;
    using CloudStorageORM.Infrastructure;
    using CloudStorageORM.Options;
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
            services.AddSingleton(storageOptions);
            services.AddDbContext<CloudStorageDbContext>((serviceProvider, options) =>
            {
                var storageOptions = serviceProvider.GetRequiredService<CloudStorageOptions>();
                options.UseCloudStorageORM(builder =>
                {
                    builder.Provider = storageOptions.Provider;
                    builder.ConnectionString = storageOptions.ConnectionString;
                    builder.ContainerName = storageOptions.ContainerName;
                });
            });

            // Register the necessary services for CloudStorageORM
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseProvider, CloudStorageDatabaseProvider>());
            services.TryAddSingleton<IDbContextTransactionManager, CloudStorageTransactionManager>();
            services.TryAddSingleton<IDatabaseCreator, CloudStorageDatabaseCreator>();
            services.TryAddSingleton<IModelSource, ModelSource>();
            services.TryAddSingleton<IModelRuntimeInitializer, ModelRuntimeInitializer>();
            services.TryAddSingleton<IDbSetInitializer, CloudStorageDbSetInitializer>();
            services.TryAddSingleton<ISingletonOptionsInitializer, SingletonOptionsInitializer>();

            // Register the necessary DbContext services
            services.TryAddSingleton<IDbContextServices, CloudStorageDbContextServices>();

            return services;
        }
    }
}
