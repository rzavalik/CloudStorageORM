namespace CloudStorageORM.Infrastructure
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using CloudStorageORM.Interfaces.Infrastructure;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Repositories;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
    using Microsoft.EntityFrameworkCore.Internal;

    public class CloudStorageDbSetSource : IDbSetSource
    {
        private readonly IStorageProvider _storageProvider;
        private readonly IBlobPathResolver _blobPathResolver;
        private readonly CloudStorageDatabase _database;

        public CloudStorageDbSetSource(
            IStorageProvider storageProvider,
            CloudStorageDatabase database)
        {
            _storageProvider = storageProvider;
            _blobPathResolver = new BlobPathResolver(storageProvider);
            _database = database;
        }

        public object Create(DbContext context, Type entityType)
        {
            Console.WriteLine($"Creating CloudStorageDbSet for {entityType.Name}");

            var type = typeof(CloudStorageDbSet<>).MakeGenericType(entityType);
            return Activator.CreateInstance(
                type,
                _storageProvider,
                _blobPathResolver,
                new CurrentDbContext(context),
                _database
            )!;
        }

        public object Create(
            DbContext context,
            string name,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.Interfaces)] Type type)
        {
            Console.WriteLine($"Creating CloudStorageDbSet for {type.Name}");

            return Create(context, type);
        }
    }
}