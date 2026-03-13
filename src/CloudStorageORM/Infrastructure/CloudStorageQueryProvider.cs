using System.Linq.Expressions;
using System.Reflection;
using CloudStorageORM.Interfaces.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;

namespace CloudStorageORM.Infrastructure;

public class CloudStorageQueryProvider(
    CloudStorageDatabase database,
    IBlobPathResolver blobPathResolver)
    : IAsyncQueryProvider
{
    private readonly CloudStorageDatabase _database = database ?? throw new ArgumentNullException(nameof(database));
    private readonly IBlobPathResolver _blobPathResolver = blobPathResolver ?? throw new ArgumentNullException(nameof(blobPathResolver));

    private Task<IList<T>> LoadEntitiesAsync<T>() where T : class
    {
        return _database.ToListAsync<T>(
            _blobPathResolver.GetBlobName(typeof(T))
        );
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
        return ExecuteCoreAsync<object>(expression).GetAwaiter().GetResult();
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return ExecuteCoreAsync<TResult>(expression).GetAwaiter().GetResult();
    }

    public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        return await ExecuteCoreAsync<TResult>(expression);
    }

    TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        return Execute<TResult>(expression);
    }

    private async Task<TResult> ExecuteCoreAsync<TResult>(Expression expression)
    {
        var elementType = ResolveEntityType(expression, typeof(TResult));
        var executeMethod = typeof(CloudStorageQueryProvider)
            .GetMethod(nameof(ExecuteTypedAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(elementType, typeof(TResult));

        var task = (Task)executeMethod.Invoke(this, [expression])!;
        await task.ConfigureAwait(false);
        var resultProperty = task.GetType().GetProperty("Result")!;
        return (TResult)resultProperty.GetValue(task)!;
    }

    private async Task<TResult> ExecuteTypedAsync<TEntity, TResult>(Expression expression)
        where TEntity : class
    {
        if (TryExtractPrimaryKeyLookup<TEntity>(expression, out var keyValue)
            && keyValue is not null)
        {
            var entity = await _database.TryLoadByPrimaryKeyAsync<TEntity>(keyValue).ConfigureAwait(false);
            return ConvertSingleLookupResult<TResult, TEntity>(entity);
        }

        var list = await LoadEntitiesAsync<TEntity>().ConfigureAwait(false);
        var inMemoryQueryable = list.AsQueryable();
        var rewrittenExpression = new QueryRootReplacementVisitor(this, inMemoryQueryable).Visit(expression);

        // For sequence-returning expressions (Where, OrderBy, etc.) use CreateQuery so
        // the EnumerableQuery provider doesn't try to box an IQueryable<T> into a TResult.
        var resultType = typeof(TResult);
        if (IsEnumerableResult(resultType))
        {
            var resultQueryable = inMemoryQueryable.Provider.CreateQuery<TEntity>(rewrittenExpression);
            return (TResult)resultQueryable;
        }

        // For scalar/singleton results (First, Any, Count, …) invoke the generic Execute.
        var executeGeneric = typeof(IQueryProvider)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m is { Name: nameof(IQueryProvider.Execute), IsGenericMethod: true })
            .MakeGenericMethod(resultType);

        return (TResult)executeGeneric.Invoke(inMemoryQueryable.Provider, [rewrittenExpression])!;
    }

    private static Type ResolveEntityType(Expression expression, Type resultType)
    {
        var finder = new QueryEntityTypeFinder();
        finder.Visit(expression);
        if (finder.EntityType is not null)
        {
            return finder.EntityType;
        }

        return resultType.IsGenericType ? resultType.GetGenericArguments().First() : resultType;
    }

    private bool TryExtractPrimaryKeyLookup<TEntity>(Expression expression, out object? keyValue)
        where TEntity : class
    {
        keyValue = null;

        if (expression is not MethodCallExpression methodCall
            || methodCall.Method.DeclaringType != typeof(Queryable)
            || methodCall.Method.Name is not (nameof(Queryable.First)
                or nameof(Queryable.FirstOrDefault)
                or nameof(Queryable.Single)
                or nameof(Queryable.SingleOrDefault)))
        {
            return false;
        }

        var predicate = ExtractPredicate(methodCall);
        if (predicate is null)
        {
            return false;
        }

        var entityType = _database.Model.FindEntityType(typeof(TEntity));
        var primaryKeyName = entityType?.FindPrimaryKey()?.Properties.FirstOrDefault()?.Name;
        if (string.IsNullOrWhiteSpace(primaryKeyName))
        {
            return false;
        }

        if (!TryExtractMemberEqualityValue(predicate, out var memberName, out var rawValue)
            || !string.Equals(memberName, primaryKeyName, StringComparison.Ordinal))
        {
            return false;
        }

        keyValue = rawValue;
        return true;
    }

    private static LambdaExpression? ExtractPredicate(MethodCallExpression methodCall)
    {
        return methodCall.Arguments.Count switch
        {
            2 => UnwrapLambda(methodCall.Arguments[1]),
            1 when methodCall.Arguments[0] is MethodCallExpression { Method.Name: nameof(Queryable.Where), Arguments.Count: 2 }
                sourceCall => UnwrapLambda(sourceCall.Arguments[1]),
            _ => null
        };
    }

    private static LambdaExpression? UnwrapLambda(Expression expression)
    {
        if (expression is UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression quoted })
        {
            return quoted;
        }

        return expression as LambdaExpression;
    }

    private static bool TryExtractMemberEqualityValue(LambdaExpression predicate, out string memberName, out object? value)
    {
        memberName = string.Empty;
        value = null;

        if (predicate.Body is not BinaryExpression { NodeType: ExpressionType.Equal } equal)
        {
            return false;
        }

        if (!TryResolveMemberAndValue(equal.Left, equal.Right, out memberName, out value)
            && !TryResolveMemberAndValue(equal.Right, equal.Left, out memberName, out value))
        {
            return false;
        }

        return true;
    }

    private static bool TryResolveMemberAndValue(Expression memberCandidate, Expression valueCandidate, out string memberName, out object? value)
    {
        memberName = string.Empty;
        value = null;

        if (memberCandidate is not MemberExpression { Expression: ParameterExpression } memberExpression)
        {
            return false;
        }

        try
        {
            var boxedValue = Expression.Convert(valueCandidate, typeof(object));
            value = Expression.Lambda<Func<object?>>(boxedValue).Compile().Invoke();
            memberName = memberExpression.Member.Name;
            return true;
        }
        catch
        {
            // Fallback to full in-memory evaluation when predicate value extraction is not directly compilable.
            return false;
        }
    }

    private static TResult ConvertSingleLookupResult<TResult, TEntity>(TEntity? entity)
        where TEntity : class
    {
        return (TResult)(object?)entity!;
    }

    private static bool IsEnumerableResult(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var def = type.GetGenericTypeDefinition();
        return def == typeof(IEnumerable<>)
            || def == typeof(IQueryable<>)
            || def == typeof(IOrderedQueryable<>)
            || def == typeof(IOrderedEnumerable<>)
            || def == typeof(IAsyncEnumerable<>);
    }

    private sealed class QueryRootReplacementVisitor(CloudStorageQueryProvider provider, IQueryable replacement)
        : ExpressionVisitor
    {
        protected override Expression VisitExtension(Expression node)
        {
            if (node is QueryRootExpression queryRoot
                && queryRoot.ElementType == replacement.ElementType)
            {
                return Expression.Constant(replacement);
            }

            return base.VisitExtension(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable queryable
                && ReferenceEquals(queryable.Provider, provider)
                && queryable.ElementType == replacement.ElementType)
            {
                return Expression.Constant(replacement);
            }

            return base.VisitConstant(node);
        }
    }

    private sealed class QueryEntityTypeFinder : ExpressionVisitor
    {
        public Type? EntityType { get; private set; }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (EntityType is null && node.Value is IQueryable queryable)
            {
                EntityType = queryable.ElementType;
            }

            return base.VisitConstant(node);
        }
    }
}
