namespace CloudStorageORM.Infrastructure
{
    using System.Linq.Expressions;
    using CloudStorageORM.Abstractions;
    using Microsoft.EntityFrameworkCore.Query;
    using Microsoft.EntityFrameworkCore.Update;

    public class CloudStorageQueryProvider : IAsyncQueryProvider
    {
        private readonly CloudStorageDatabase _database;

        public CloudStorageQueryProvider(CloudStorageDatabase database)
        {
            _database = database;
        }

        public Task<IList<T>> LoadEntitiesAsync<T>()
        {
            return _database.ToListAsync<T>(GetBlobName(typeof(T)));
        }

        private static string GetBlobName(Type type)
        {
            var blobAttr = type.GetCustomAttributes(typeof(BlobSettingsAttribute), false)
                                     .Cast<BlobSettingsAttribute>()
                                     .FirstOrDefault();
            return blobAttr?.Name ?? type.Name.ToLowerInvariant().Trim();
        }

        private static string GetPath(IUpdateEntry entry)
        {
            var blobName = GetBlobName(entry.EntityType.ClrType);
            var keyProperty = entry.EntityType.FindPrimaryKey()?.Properties.FirstOrDefault();
            var keyValue = entry.GetCurrentValue(keyProperty!);

            if (string.IsNullOrWhiteSpace(keyValue?.ToString()))
            {
                throw new InvalidOperationException($"Cannot persist entity '{entry.EntityType.Name}' without a valid key value.");
            }

            return $"{blobName}/{keyValue}.json";
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetGenericArguments().First();
            var queryableType = typeof(CloudStorageQueryable<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(queryableType, this, expression)!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new CloudStorageQueryable<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            return ExecuteAsync<object>(expression).Result;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return ExecuteAsync<TResult>(expression).Result;
        }

        public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var entityType = typeof(TResult);

            // If TResult is IAsyncEnumerable<T>
            if (typeof(TResult).IsGenericType &&
                typeof(TResult).GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            {
                var elementType = typeof(TResult).GetGenericArguments()[0];

                var loadMethod = typeof(CloudStorageDatabase)
                    .GetMethod(nameof(CloudStorageDatabase.LoadEntitiesAsync), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .MakeGenericMethod(elementType);

                var task = (Task)loadMethod.Invoke(_database, Array.Empty<object>())!;
                await task.ConfigureAwait(false);

                var resultProperty = task.GetType().GetProperty("Result")!;
                var list = (IEnumerable<object>)resultProperty.GetValue(task)!;

                var toAsyncEnumerableMethod = typeof(CloudStorageQueryProvider)
                    .GetMethod(nameof(ToAsyncEnumerable), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .MakeGenericMethod(elementType);

                return (TResult)toAsyncEnumerableMethod.Invoke(this, new object[] { list })!;
            }
            else
            {
                // Load synchronously
                var loadMethod = typeof(CloudStorageDatabase)
                    .GetMethod(nameof(CloudStorageDatabase.LoadEntitiesAsync), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .MakeGenericMethod(typeof(TResult).GetGenericArguments().FirstOrDefault() ?? typeof(TResult));

                var task = (Task)loadMethod.Invoke(_database, Array.Empty<object>())!;
                task.Wait();

                var resultProperty = task.GetType().GetProperty("Result")!;
                var list = (IEnumerable<object>)resultProperty.GetValue(task)!;

                var asQueryableMethod = typeof(Queryable)
                    .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .First(m => m.Name == nameof(Queryable.AsQueryable)
                             && m.IsGenericMethodDefinition
                             && m.GetParameters().Length == 1
                             && m.GetParameters()[0].ParameterType.IsGenericType
                             && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    .MakeGenericMethod(typeof(TResult).GetGenericArguments().FirstOrDefault() ?? typeof(TResult));

                var queryable = asQueryableMethod.Invoke(null, new object[] { list });

                return (TResult)queryable!;
            }
        }

        TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            return ExecuteAsync<TResult>(expression, cancellationToken).Result;
        }

        private async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<object> list)
        {
            foreach (var item in list)
            {
                yield return (T)item;
            }
            await Task.CompletedTask;
        }
    }
}
