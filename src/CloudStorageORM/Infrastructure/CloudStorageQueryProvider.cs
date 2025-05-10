namespace CloudStorageORM.Infrastructure
{
    using System.Linq;
    using System.Linq.Expressions;
    using CloudStorageORM.Interfaces.Infrastructure;
    using Microsoft.EntityFrameworkCore.Query;

    public class CloudStorageQueryProvider : IAsyncQueryProvider
    {
        private readonly CloudStorageDatabase _database;
        private readonly IBlobPathResolver _blobPathResolver;

        public CloudStorageQueryProvider(
            CloudStorageDatabase database,
            IBlobPathResolver blobPathResolver)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _blobPathResolver = blobPathResolver ?? throw new ArgumentNullException(nameof(blobPathResolver));
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

            if (elementType.IsGenericType &&
                (elementType.GetGenericTypeDefinition() == typeof(Task<>) ||
                 elementType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
            {
                elementType = elementType.GetGenericArguments()[0];
            }

            var queryableType = typeof(CloudStorageQueryable<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(queryableType, this, expression)!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            var tElement = typeof(TElement);

            if (tElement.IsGenericType && tElement.GetGenericTypeDefinition() == typeof(Task<>))
            {
                tElement = tElement.GetGenericArguments()[0];
            }

            if (tElement.IsGenericType && tElement.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                tElement = tElement.GetGenericArguments()[0];
            }

            var queryableType = typeof(CloudStorageQueryable<>).MakeGenericType(tElement);
            var instance = (IQueryable)Activator.CreateInstance(queryableType, this, expression)!;

            return Queryable.Cast<TElement>(instance);
        }

        public object Execute(Expression expression)
        {
            var lambda = Expression.Lambda(expression);
            var compiled = lambda.Compile();
            return compiled.DynamicInvoke();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var tResult = typeof(TResult);

            if (expression is MethodCallExpression methodCall && methodCall.Method.Name == "FirstOrDefault")
            {
                var entityType = methodCall.Arguments[0].Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                var blobName = _blobPathResolver.GetBlobName(entityType);
                var method = typeof(CloudStorageDatabase)
                    .GetMethod(nameof(CloudStorageDatabase.ToListAsync))!
                    .MakeGenericMethod(entityType);

                var task = (Task)method.Invoke(_database, new object[] { blobName })!;
                task.Wait();

                var list = (IEnumerable<object>)task.GetType().GetProperty("Result")!.GetValue(task)!;

                if (methodCall.Arguments.Count == 2)
                {
                    var lambda = ((UnaryExpression)methodCall.Arguments[1]).Operand as LambdaExpression;
                    var compiled = lambda!.Compile();
                    var result = list.FirstOrDefault(e => (bool)compiled.DynamicInvoke(e)!);
                    return (TResult)result!;
                }
                else
                {
                    var result = list.FirstOrDefault();
                    if (result is TResult typedResult)
                        return typedResult;

                    if (result == null)
                        return default!;

                    throw new InvalidCastException($"Cannot cast result of type {result.GetType()} to expected type {typeof(TResult)}");
                }
            }
            else if (tResult.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(tResult.GetGenericTypeDefinition()))
            {
                var entityType = tResult.GetGenericArguments()[0];
                var blobName = _blobPathResolver.GetBlobName(entityType);

                var method = typeof(CloudStorageDatabase)
                    .GetMethod(nameof(CloudStorageDatabase.ToListAsync))!
                    .MakeGenericMethod(entityType);

                var task = (Task)method.Invoke(_database, new object[] { blobName })!;
                task.Wait();

                var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
                return (TResult)result;
            }
            else if (tResult.IsGenericType &&
                (tResult.GetGenericTypeDefinition() == typeof(List<>) ||
                 tResult.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                var entityType = tResult.GetGenericArguments()[0];
                var blobName = _blobPathResolver.GetBlobName(entityType);

                var method = typeof(CloudStorageDatabase)
                    .GetMethod(nameof(CloudStorageDatabase.ToListAsync))!
                    .MakeGenericMethod(entityType);

                var task = (Task)method.Invoke(_database, new object[] { blobName })!;
                task.Wait();

                var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
                return (TResult)result;
            }
            else
            {
                var entityType = typeof(TResult);
                var blobName = _blobPathResolver.GetBlobName(entityType);
                var method = typeof(CloudStorageDatabase)
                    .GetMethod(nameof(CloudStorageDatabase.ToListAsync))!
                    .MakeGenericMethod(entityType);

                var task = (Task)method.Invoke(_database, new object[] { blobName })!;
                task.Wait();

                var list = (IEnumerable<object>)task.GetType().GetProperty("Result")!.GetValue(task)!;

                var first = list.FirstOrDefault();
                if (first == null) return default!;
                if (first is TResult exact) return exact;

                throw new InvalidCastException($"Final fallback: cannot cast {first?.GetType()} to {typeof(TResult)}");
            }
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var tResult = typeof(TResult);

            if (tResult.IsGenericType && tResult.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var innerType = tResult.GetGenericArguments()[0];

                if (innerType.IsGenericType && innerType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    innerType = innerType.GetGenericArguments()[0];
                }

                if (innerType.IsGenericType && innerType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var entityType = innerType.GetGenericArguments()[0];
                    var blobName = _blobPathResolver.GetBlobName(entityType);

                    var method = typeof(CloudStorageDatabase)
                        .GetMethod(nameof(CloudStorageDatabase.ToListAsync))!
                        .MakeGenericMethod(entityType);

                    var task = (Task)method.Invoke(_database, new object[] { blobName })!;
                    task.Wait();

                    var list = task.GetType().GetProperty("Result")!.GetValue(task)!;
                    var resultTask = typeof(Task).GetMethod(nameof(Task.FromResult))!
                        .MakeGenericMethod(innerType)
                        .Invoke(null, new[] { list });

                    return (TResult)resultTask!;
                }

                if (expression is MethodCallExpression methodCall && methodCall.Method.Name == "FirstOrDefault")
                {
                    var blobName = _blobPathResolver.GetBlobName(innerType);
                    var method = typeof(CloudStorageDatabase)
                        .GetMethod(nameof(CloudStorageDatabase.ToListAsync))!
                        .MakeGenericMethod(innerType);

                    var task = (Task)method.Invoke(_database, new object[] { blobName })!;
                    task.Wait();

                    var list = (IEnumerable<object>)task.GetType().GetProperty("Result")!.GetValue(task)!;

                    object? result = null;

                    if (methodCall.Arguments.Count == 2)
                    {
                        var lambda = ((UnaryExpression)methodCall.Arguments[1]).Operand as LambdaExpression;
                        var compiled = lambda!.Compile();
                        result = list.FirstOrDefault(e => (bool)compiled.DynamicInvoke(e)!);
                    }
                    else
                    {
                        result = list.FirstOrDefault();
                    }

                    var fromResultMethod = typeof(Task).GetMethods()
                        .First(m => m.Name == "FromResult" && m.IsGenericMethod)!
                        .MakeGenericMethod(innerType);

                    return (TResult)fromResultMethod.Invoke(null, new[] { result })!;
                }
            }

            if (tResult.IsGenericType && tResult.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            {
                var entityType = tResult.GetGenericArguments()[0];
                var queryableType = typeof(CloudStorageQueryable<>).MakeGenericType(entityType);
                var instance = Activator.CreateInstance(queryableType, this, expression);
                return (TResult)instance!;
            }

            if (expression is ConstantExpression constExpr && typeof(IAsyncEnumerable<>).MakeGenericType(constExpr.Type.GetGenericArguments()[0]).IsAssignableFrom(tResult))
            {
                return (TResult)constExpr.Value!;
            }

            if (typeof(IQueryable).IsAssignableFrom(tResult) ||
                (tResult.IsGenericType && tResult.GetGenericTypeDefinition() == typeof(Task<>) &&
                 typeof(IQueryable).IsAssignableFrom(tResult.GetGenericArguments()[0])))
            {
                var entityType = tResult.IsGenericType
                    ? tResult.GetGenericArguments()[0].GetGenericArguments().FirstOrDefault() ?? typeof(object)
                    : tResult.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                var queryableType = typeof(CloudStorageQueryable<>).MakeGenericType(entityType);
                var instance = Activator.CreateInstance(queryableType, this, expression);

                if (tResult.IsGenericType && tResult.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!
                        .MakeGenericMethod(tResult.GetGenericArguments()[0]);
                    return (TResult)fromResult.Invoke(null, new[] { instance })!;
                }

                return (TResult)instance!;
            }

            throw new NotSupportedException($"ExecuteAsync does not support expression: {expression}");
        }
    }
}