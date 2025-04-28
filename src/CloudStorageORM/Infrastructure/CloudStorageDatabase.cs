namespace CloudStorageORM.Infrastructure
{
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Metadata;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.EntityFrameworkCore.Update;
    using System;
    using Microsoft.EntityFrameworkCore.Query;
    using System.Linq.Expressions;
    using Azure.Storage.Blobs;
    using System.Text.Json;
    using System.Linq;
    using Microsoft.EntityFrameworkCore;
    using CloudStorageORM.Options;
    using System.Reflection;
    using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

    public class CloudStorageDatabase : IDatabase
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly CloudStorageOptions _options;

        public IModel Model { get; }
        public IDatabaseCreator Creator { get; }
        public IExecutionStrategyFactory ExecutionStrategyFactory { get; }

        public CloudStorageDatabase(
            IModel model,
            IDatabaseCreator databaseCreator,
            IExecutionStrategyFactory executionStrategyFactory,
            BlobServiceClient blobServiceClient,
            CloudStorageOptions options)
        {
            Model = model;
            Creator = databaseCreator;
            ExecutionStrategyFactory = executionStrategyFactory;
            _blobServiceClient = blobServiceClient;
            _options = options;
        }

        public Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
            => Creator.EnsureCreatedAsync(cancellationToken);

        public Task EnsureDeletedAsync(CancellationToken cancellationToken = default)
            => Creator.EnsureDeletedAsync(cancellationToken);

        public int SaveChanges(IList<IUpdateEntry> entries)
        {
            return 0;
        }

        public Task<int> SaveChangesAsync(IList<IUpdateEntry> entries, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        Func<QueryContext, TResult> IDatabase.CompileQuery<TResult>(Expression query, bool async)
        {
            return queryContext =>
            {
                Type entityType;

                if (typeof(TResult).IsGenericType &&
                    typeof(TResult).GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
                {
                    entityType = typeof(TResult).GetGenericArguments()[0];
                }
                else
                {
                    entityType = typeof(TResult);
                }

                var provider = new CloudStorageQueryProvider(this);
                var queryableType = typeof(CloudStorageQueryable<>).MakeGenericType(entityType);

                var queryable = (IQueryable)Activator.CreateInstance(queryableType, provider)!;

                return (TResult)(object)queryable;
            };
        }

        //Func<QueryContext, TResult> IDatabase.CompileQuery<TResult>(Expression query, bool async)
        //{
        //    return queryContext =>
        //    {
        //        Type entityType;
        //        var expectsAsync = false;

        //        if (typeof(TResult).IsGenericType &&
        //            typeof(TResult).GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
        //        {
        //            entityType = typeof(TResult).GetGenericArguments()[0];
        //            expectsAsync = true;
        //        }
        //        else
        //        {
        //            entityType = typeof(TResult);
        //        }

        //        var methodInfo = typeof(CloudStorageDatabase)
        //            .GetMethod(nameof(LoadEntitiesAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
        //            .MakeGenericMethod(entityType);

        //        var task = (Task)methodInfo.Invoke(this, Array.Empty<object>())!;
        //        task.Wait();

        //        var resultProperty = task.GetType().GetProperty("Result")!;
        //        var loadedEntities = resultProperty.GetValue(task)!;

        //        if (expectsAsync)
        //        {
        //            var toAsyncEnumerableMethod = typeof(CloudStorageDatabase)
        //                .GetMethod(nameof(ToAsyncEnumerable), BindingFlags.NonPublic | BindingFlags.Instance)!
        //                .MakeGenericMethod(entityType);

        //            var asyncEnumerable = toAsyncEnumerableMethod.Invoke(this, new object[] { loadedEntities });

        //            return (TResult)asyncEnumerable!;
        //        }
        //        else
        //        {
        //            var asQueryableMethod = typeof(Queryable)
        //                .GetMethods(BindingFlags.Public | BindingFlags.Static)
        //                .First(m => m.Name == nameof(Queryable.AsQueryable)
        //                         && m.IsGenericMethodDefinition
        //                         && m.GetParameters().Length == 1
        //                         && m.GetParameters()[0].ParameterType.IsGenericType
        //                         && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        //                .MakeGenericMethod(entityType);

        //            var queryable = asQueryableMethod.Invoke(null, new object[] { loadedEntities });

        //            return (TResult)queryable!;
        //        }
        //    };
        //}

        internal async Task<List<TEntity>> LoadEntitiesAsync<TEntity>()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);

            var results = new List<TEntity>();

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var download = await blobClient.DownloadContentAsync();

                var entity = JsonSerializer.Deserialize<TEntity>(download.Value.Content.ToStream());

                if (entity != null)
                {
                    results.Add(entity);
                }
            }

            return results;
        }

        private async IAsyncEnumerable<TEntity> ToAsyncEnumerable<TEntity>(IQueryable source)
        {
            foreach (var item in source)
            {
                yield return (TEntity)item!;
            }
            await Task.CompletedTask;
        }

        public async Task<List<TEntity>> ToListAsync<TEntity>(string containerName)
        {
            var list = new List<TEntity>();
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var downloadInfo = await blobClient.DownloadContentAsync();

                var entity = JsonSerializer.Deserialize<TEntity>(downloadInfo.Value.Content.ToStream());
                if (entity != null)
                    list.Add(entity);
            }

            return list;
        }

        public Expression<Func<QueryContext, TResult>> CompileQueryExpression<TResult>(Expression query, bool async, IReadOnlySet<string> nonNullableReferenceTypeParameters)
        {
            throw new NotImplementedException();
        }
    }
}
