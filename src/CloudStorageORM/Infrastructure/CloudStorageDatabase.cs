namespace CloudStorageORM.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using CloudStorageORM.Interfaces.Infrastructure;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.EntityFrameworkCore.Query;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.EntityFrameworkCore.Update;

    public class CloudStorageDatabase : IDatabase
    {
        private readonly DbContext _context;
        private readonly CloudStorageOptions _options;
        private readonly IStorageProvider _storageProvider;
        private readonly IBlobPathResolver _blobPathResolver;

        public IModel Model { get; }
        public IDatabaseCreator Creator { get; }
        public IExecutionStrategyFactory ExecutionStrategyFactory { get; }

        public CloudStorageDatabase(
            IModel model,
            IDatabaseCreator databaseCreator,
            IExecutionStrategyFactory executionStrategyFactory,
            IStorageProvider storageProvider,
            CloudStorageOptions options,
            ICurrentDbContext currentDbContext)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Creator = databaseCreator ?? throw new ArgumentNullException(nameof(databaseCreator));
            ExecutionStrategyFactory = executionStrategyFactory ?? throw new ArgumentNullException(nameof(executionStrategyFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
            _context = currentDbContext.Context ?? throw new ArgumentNullException(nameof(currentDbContext));
            _blobPathResolver = new BlobPathResolver(_storageProvider);
        }

        public Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
            => Creator.EnsureCreatedAsync(cancellationToken);

        public Task EnsureDeletedAsync(CancellationToken cancellationToken = default)
            => Creator.EnsureDeletedAsync(cancellationToken);

        public int SaveChanges(IList<IUpdateEntry> entries)
        {
            var request = ProcessChangesAsync(entries);
            request.Wait();
            return request.Result;
        }

        public async Task<int> SaveChangesAsync(IList<IUpdateEntry> entries, CancellationToken cancellationToken = default)
        {
            return await ProcessChangesAsync(entries);
        }

        private async Task<int> ProcessChangesAsync(IList<IUpdateEntry> entries)
        {
            var changes = 0;

            foreach (var entry in entries)
            {
                var entity = ((Microsoft.EntityFrameworkCore.ChangeTracking.Internal.InternalEntityEntry)entry).Entity;
                var path = _blobPathResolver.GetPath(entry);

                var entityType = entry.EntityType.ClrType;
                var key = entry.EntityType.FindPrimaryKey();
                var keyProps = key?.Properties;

                if (keyProps != null)
                {
                    var existingTracked = entry.Context?.ChangeTracker.Entries()
                        .FirstOrDefault(e =>
                            e.Entity != null &&
                            e.Entity.GetType() == entityType &&
                            !ReferenceEquals(e.Entity, entity) &&
                            keyProps.All(p =>
                                Equals(p.PropertyInfo?.GetValue(e.Entity), p.PropertyInfo?.GetValue(entity))));

                    if (existingTracked != null)
                    {
                        entry.Context.Entry(existingTracked.Entity).State = EntityState.Detached;
                    }
                }

                var tracked = entry.Context?.ChangeTracker
                    .Entries()
                    .Any(e =>
                        e.Entity != null &&
                        e.Entity.GetType() == entity.GetType() &&
                        KeysMatch(e, entry));

                if (tracked != true)
                {
                    continue;
                }

                switch (entry.EntityState)
                {
                    case EntityState.Added:
                    case EntityState.Modified:
                        await _storageProvider.SaveAsync(path, entity);
                        changes++;
                        break;

                    case EntityState.Deleted:
                        await _storageProvider.DeleteAsync(path);
                        changes++;
                        break;
                }
            }

            return changes;
        }

        private bool KeysMatch(EntityEntry trackedEntry, IUpdateEntry newEntry)
        {
            var keyProps = newEntry.EntityType.FindPrimaryKey()?.Properties;
            if (keyProps == null)
            {
                return false;
            }

            var entity = ((Microsoft.EntityFrameworkCore.ChangeTracking.Internal.InternalEntityEntry)newEntry).Entity;

            foreach (var keyProp in keyProps)
            {
                var trackedValue = keyProp.PropertyInfo?.GetValue(trackedEntry.Entity);
                var newValue = keyProp.PropertyInfo?.GetValue(entity);

                if (!object.Equals(trackedValue, newValue))
                {
                    return false;
                }
            }

            return true;
        }

        public Func<QueryContext, TResult> CompileQuery<TResult>(Expression query, bool async)
        {
            return _ => ExecuteCompiledQuery<TResult>(query);
        }

        public Expression<Func<QueryContext, TResult>> CompileQueryExpression<TResult>(Expression query, bool async, IReadOnlySet<string> nonNullableReferenceTypeParameters)
        {
            return _ => ExecuteCompiledQuery<TResult>(query);
        }

        private TResult ExecuteCompiledQuery<TResult>(Expression query)
        {
            var resultType = typeof(TResult);

            if (resultType.IsGenericType)
            {
                var genericDef = resultType.GetGenericTypeDefinition();
                var elementType = resultType.GetGenericArguments()[0];

                if (genericDef == typeof(Task<>))
                {
                    var blobName = _blobPathResolver.GetBlobName(elementType);
                    var method = typeof(CloudStorageDatabase).GetMethod(nameof(ToListAsync))!.MakeGenericMethod(elementType);
                    var task = (Task)method.Invoke(this, new object[] { blobName })!;
                    task.Wait();
                    var list = (IEnumerable<object>)task.GetType().GetProperty("Result")!.GetValue(task)!;
                    var result = ExecuteQuery(query, list, elementType);
                    var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(elementType);
                    return (TResult)fromResult.Invoke(null, new[] { result })!;
                }
                else if (genericDef == typeof(IQueryable<>) ||
                         genericDef == typeof(IEnumerable<>))
                {
                    var method = typeof(CloudStorageDatabase).GetMethod(nameof(ToListAsync))!.MakeGenericMethod(elementType);
                    var task = (Task)method.Invoke(this, new object[] { _blobPathResolver.GetBlobName(elementType) })!;
                    task.Wait();
                    return (TResult)task.GetType().GetProperty("Result")!.GetValue(task)!;
                }
                else if (genericDef == typeof(IAsyncEnumerable<>))
                {
                    var queryableType = typeof(CloudStorageQueryable<>).MakeGenericType(elementType);
                    return (TResult)Activator.CreateInstance(queryableType, new CloudStorageQueryProvider(this, _blobPathResolver))!;
                }
            }

            var singleType = resultType;
            var blob = _blobPathResolver.GetBlobName(singleType);
            var singleMethod = typeof(CloudStorageDatabase).GetMethod(nameof(ToListAsync))!.MakeGenericMethod(singleType);
            var t = (Task)singleMethod.Invoke(this, new object[] { blob })!;
            t.Wait();
            var l = (IEnumerable<object>)t.GetType().GetProperty("Result")!.GetValue(t)!;
            var r = ExecuteQuery(query, l, singleType);
            return (TResult)r!;
        }

        private object? ExecuteQuery(Expression query, IEnumerable<object> list, Type elementType)
        {
            if (query is MethodCallExpression mc && mc.Method.Name == "FirstOrDefault")
            {
                if (mc.Arguments.Count == 2)
                {
                    var lambda = (LambdaExpression)((UnaryExpression)mc.Arguments[1]).Operand;
                    var inlined = PartialEvaluator.Evaluate(lambda);
                    var compiled = inlined.Compile();
                    foreach (var item in list)
                    {
                        var result = compiled.DynamicInvoke(item);
                        if (result is bool b && b)
                            return item;
                    }
                    return null;
                }
                else
                {
                    return list.FirstOrDefault();
                }
            }
            else if (query is MethodCallExpression mc2 && mc2.Method.Name == "SingleOrDefault")
            {
                if (mc2.Arguments.Count == 2)
                {
                    var lambda = (LambdaExpression)((UnaryExpression)mc2.Arguments[1]).Operand;
                    var evaluated = EvaluateClosureLambda(lambda);
                    var compiled = evaluated.Compile();
                    return list.SingleOrDefault(e => (bool)compiled.DynamicInvoke(e)!);
                }
                else
                {
                    return list.SingleOrDefault();
                }
            }

            throw new NotSupportedException("Query execution not supported for type: " + elementType.Name);
        }

        public async Task<IList<TEntity>> ToListAsync<TEntity>(string containerName)
        {
            var files = await _storageProvider.ListAsync(containerName);
            var results = new List<TEntity>();

            var entityType = _context.Model.FindEntityType(typeof(TEntity));
            var keyProperties = entityType?.FindPrimaryKey()?.Properties;

            foreach (var file in files)
            {
                var entity = await _storageProvider.ReadAsync<TEntity>(file);
                if (entity is null) continue;

                var existingTracked = _context.ChangeTracker.Entries()
                    .FirstOrDefault(e => keyProperties != null && keyProperties.All(p =>
                        Equals(p.PropertyInfo?.GetValue(e.Entity), p.PropertyInfo?.GetValue(entity))));

                if (existingTracked != null)
                {
                    _context.Entry(existingTracked.Entity).State = EntityState.Detached;
                }

                _context.Attach(entity);
                results.Add(entity);
            }

            return results;
        }

        private static LambdaExpression EvaluateClosureLambda(LambdaExpression lambda)
        {
            var constants = new Dictionary<string, object?>();
            new ClosureExtractor(constants).Visit(lambda.Body);
            var replaced = new ClosureReplacer(constants).Visit(lambda.Body);

            var usedParams = ExpressionExtensions.GetFreeVariables(replaced);
            var lambdaParams = lambda.Parameters.Where(p => usedParams.Contains(p)).ToList();

            return Expression.Lambda(replaced, lambdaParams);
        }

        public static class ExpressionExtensions
        {
            public static IEnumerable<ParameterExpression> GetFreeVariables(Expression expression)
            {
                var visitor = new FreeVariableVisitor();
                visitor.Visit(expression);
                return visitor.Parameters;
            }

            private class FreeVariableVisitor : ExpressionVisitor
            {
                public HashSet<ParameterExpression> Parameters { get; } = new();

                protected override Expression VisitParameter(ParameterExpression node)
                {
                    Parameters.Add(node);
                    return node;
                }

                protected override Expression VisitLambda<T>(Expression<T> node)
                {
                    foreach (var p in node.Parameters)
                        Parameters.Remove(p);

                    return base.VisitLambda(node);
                }
            }
        }

        public static class PartialEvaluator
        {
            public static LambdaExpression Evaluate(LambdaExpression expression)
            {
                var constantMap = new Dictionary<Expression, object?>();
                new ClosureExtractor(constantMap).Visit(expression.Body);

                var rewritten = new ClosureRewriter(constantMap).Visit(expression.Body);

                var declaredParams = expression.Parameters.ToHashSet();
                var usedParams = GetUsedParameters(rewritten);

                return Expression.Lambda(rewritten, usedParams);
            }

            private static HashSet<ParameterExpression> GetUsedParameters(Expression expr)
            {
                var visitor = new UsedParameterVisitor();
                visitor.Visit(expr);
                return visitor.Parameters;
            }

            private class UsedParameterVisitor : ExpressionVisitor
            {
                public HashSet<ParameterExpression> Parameters { get; } = new();

                protected override Expression VisitParameter(ParameterExpression node)
                {
                    Parameters.Add(node);
                    return base.VisitParameter(node);
                }
            }

            private class ClosureExtractor : ExpressionVisitor
            {
                private readonly Dictionary<Expression, object?> _map;

                public ClosureExtractor(Dictionary<Expression, object?> map)
                {
                    _map = map;
                }

                protected override Expression VisitMember(MemberExpression node)
                {
                    if (node.Expression is ConstantExpression closure)
                    {
                        var value = node.Member switch
                        {
                            System.Reflection.FieldInfo f => f.GetValue(closure.Value),
                            System.Reflection.PropertyInfo p => p.GetValue(closure.Value),
                            _ => throw new NotSupportedException()
                        };

                        _map[node] = value;
                    }

                    return base.VisitMember(node);
                }
            }

            private class ClosureRewriter : ExpressionVisitor
            {
                private readonly Dictionary<Expression, object?> _map;

                public ClosureRewriter(Dictionary<Expression, object?> map)
                {
                    _map = map;
                }

                protected override Expression VisitMember(MemberExpression node)
                {
                    if (_map.TryGetValue(node, out var value))
                    {
                        return Expression.Constant(value, node.Type);
                    }

                    return base.VisitMember(node);
                }
            }
        }

        private class ClosureExtractor : ExpressionVisitor
        {
            private readonly Dictionary<string, object?> _values;

            public ClosureExtractor(Dictionary<string, object?> values)
            {
                _values = values;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression is ConstantExpression closure)
                {
                    var value = node.Member switch
                    {
                        System.Reflection.FieldInfo f => f.GetValue(closure.Value),
                        System.Reflection.PropertyInfo p => p.GetValue(closure.Value),
                        _ => throw new NotSupportedException()
                    };

                    // Usa o nome completo da expressão como chave (ex: value(__closure).__p_0)
                    _values[node.ToString()!] = value;
                }

                return base.VisitMember(node);
            }
        }

        private class ClosureReplacer : ExpressionVisitor
        {
            private readonly Dictionary<string, object?> _values;

            public ClosureReplacer(Dictionary<string, object?> values)
            {
                _values = values;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression is ConstantExpression && _values.TryGetValue(node.ToString()!, out var value))
                {
                    return Expression.Constant(value, node.Type);
                }

                return base.VisitMember(node);
            }
        }

        private class CapturedParameterRewriter : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, object?> _captured;

            public CapturedParameterRewriter(Dictionary<ParameterExpression, object?> captured)
            {
                _captured = captured;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (_captured.TryGetValue(node, out var value))
                {
                    return Expression.Constant(value, node.Type);
                }

                return base.VisitParameter(node);
            }
        }

        private class ExpressionVisitorHelper : ExpressionVisitor
        {
            private readonly Dictionary<string, object?> _captured;

            public ExpressionVisitorHelper(Dictionary<string, object?> captured)
            {
                _captured = captured;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression is ConstantExpression closure)
                {
                    var value = node.Member switch
                    {
                        System.Reflection.FieldInfo field => field.GetValue(closure.Value),
                        System.Reflection.PropertyInfo prop => prop.GetValue(closure.Value),
                        _ => throw new NotSupportedException()
                    };

                    _captured[node.ToString()!] = value;
                }

                return base.VisitMember(node);
            }
        }

        private class ClosureRewriter : ExpressionVisitor
        {
            private readonly Dictionary<string, object?> _captured;

            public ClosureRewriter(Dictionary<string, object?> captured)
            {
                _captured = captured;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression is ConstantExpression && _captured.TryGetValue(node.ToString()!, out var value))
                {
                    return Expression.Constant(value, node.Type);
                }

                return base.VisitMember(node);
            }
        }

        private class CapturedVariableExtractor : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, object?> _captured;
            private readonly IReadOnlyList<ParameterExpression> _parameters;

            public CapturedVariableExtractor(Dictionary<ParameterExpression, object?> captured, IReadOnlyList<ParameterExpression> parameters)
            {
                _captured = captured;
                _parameters = parameters;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression is ConstantExpression closure)
                {
                    var value = node.Member switch
                    {
                        System.Reflection.FieldInfo field => field.GetValue(closure.Value),
                        System.Reflection.PropertyInfo prop => prop.GetValue(closure.Value),
                        _ => throw new NotSupportedException()
                    };

                    if (value is ParameterExpression p && !_parameters.Contains(p))
                    {
                        _captured[p] = value;
                    }
                }

                return base.VisitMember(node);
            }
        }

        private class CapturedVariableReplacer : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, object?> _captured;

            public CapturedVariableReplacer(Dictionary<ParameterExpression, object?> captured)
            {
                _captured = captured;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (_captured.TryGetValue(node, out var value) && value != null)
                {
                    return Expression.Constant(value, node.Type);
                }

                return base.VisitParameter(node);
            }
        }
    }
}