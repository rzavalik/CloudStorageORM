namespace CloudStorageORM.Infrastructure
{
    using CloudStorageORM.Validators;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.Extensions.DependencyInjection;

    public class CloudStorageSingletonOptionsInitializer : ISingletonOptionsInitializer
    {
        public void EnsureInitialized(IServiceProvider serviceProvider, IDbContextOptions options)
        {
        }

        public void Initialize(IServiceProvider serviceProvider, IDbContextOptions options)
        {
            var extension = options.Extensions
                .OfType<CloudStorageOrmOptionsExtension>()
                .FirstOrDefault();

            if (extension?.Options is not { } cloudOptions)
            {
                throw new InvalidOperationException("CloudStorageOptions was not provided.");
            }

            var model = serviceProvider.GetRequiredService<IModel>();
            CloudStorageModelValidator.Validate(model, cloudOptions.Provider);
        }


        public void Validate(IDbContextOptions options)
        {
        }
    }
}
