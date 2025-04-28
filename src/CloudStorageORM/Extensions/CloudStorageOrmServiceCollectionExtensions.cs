namespace CloudStorageORM.Extensions
{
    using CloudStorageORM.DbContext;
    using CloudStorageORM.Enums;
    using CloudStorageORM.Infrastructure;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public static class CloudStorageOrmServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkCloudStorageORM(this IServiceCollection services)
        {
            // Register the necessary services for CloudStorageORM
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseProvider, CloudStorageDatabaseProvider>());
            services.TryAddSingleton<IDbContextTransactionManager, CloudStorageTransactionManager>();
            services.TryAddSingleton<IDatabaseCreator, CloudStorageDatabaseCreator>();
            services.TryAddSingleton<IModelSource, ModelSource>();
            services.TryAddSingleton<IModelRuntimeInitializer, ModelRuntimeInitializer>();
            services.TryAddSingleton<IDbSetInitializer, CloudStorageDbSetInitializer>();
            services.TryAddSingleton<ISingletonOptionsInitializer, SingletonOptionsInitializer>();

            // Register CloudStorageDbContext with AddDbContext
            services.AddDbContext<CloudStorageDbContext>(options =>
            {
                options.UseCloudStorageORM(builder =>
                {
                    builder.Provider = CloudProvider.Azure;
                    builder.ConnectionString = "UseDevelopmentStorage=true";
                    builder.ContainerName = "sample-container";
                });
            });

            // Change CloudStorageDbContextServices registration to scoped
            services.TryAddScoped<IDbContextServices, CloudStorageDbContextServices>();

            return services;
        }
    }
}
