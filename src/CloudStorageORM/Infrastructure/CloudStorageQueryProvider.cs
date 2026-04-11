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

    private readonly IBlobPathResolver _blobPathResolver =
        blobPathResolver ?? throw new ArgumentNullException(nameof(blobPathResolver));

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

    public async Task<TResult> ExecuteAsync<TResult>(Expression expression,
        CancellationToken cancellationToken = default)
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
        if (TryExtractPrimaryKeyConstraint<TEntity>(expression, out var keyConstraint))
        {
            switch (keyConstraint)
            {
                case { IsEquality: true, EqualityValue: not null }:
                {
                    var entity = await _database.TryLoadByPrimaryKeyAsync<TEntity>(keyConstraint.EqualityValue)
                        .ConfigureAwait(false);
                    return ConvertSingleLookupResult<TResult, TEntity>(entity);
                }
                case { IsEquality: false, HasRangeBound: true }:
                {
                    var rangedList = await _database
                        .LoadByPrimaryKeyRangeAsync<TEntity>(
                            keyConstraint.LowerBound,
                            keyConstraint.LowerInclusive,
                            keyConstraint.UpperBound,
                            keyConstraint.UpperInclusive)
                        .ConfigureAwait(false);

                    return ExecuteAgainstInMemory<TResult, TEntity>(expression, rangedList);
                }
            }
        }

        var list = await LoadEntitiesAsync<TEntity>().ConfigureAwait(false);
        return ExecuteAgainstInMemory<TResult, TEntity>(expression, list);
    }

    private TResult ExecuteAgainstInMemory<TResult, TEntity>(Expression expression, IList<TEntity> list)
        where TEntity : class
    {
        var inMemoryQueryable = list.AsQueryable();
        var rewrittenExpression = new QueryRootReplacementVisitor(this, inMemoryQueryable).Visit(expression);
        rewrittenExpression = new ExtensionReducingVisitor().Visit(rewrittenExpression);

        // For sequence-returning expressions (Where, OrderBy, etc.) use CreateQuery so
        // the EnumerableQuery provider doesn't try to box an IQueryable<T> into a TResult.
        var resultType = typeof(TResult);
        if (IsEnumerableResult(resultType) || resultType == typeof(object) && typeof(IEnumerable<TEntity>).IsAssignableFrom(rewrittenExpression.Type))
        {
            var resultQueryable = inMemoryQueryable.Provider.CreateQuery<TEntity>(rewrittenExpression);
            return (TResult)resultQueryable;
        }

        if (TryExecuteScalarFallback<TResult, TEntity>(expression, list, out var scalarFallback))
        {
            return scalarFallback;
        }

        // For scalar/singleton results (First, Any, Count, …) invoke the generic Execute.
        var executeGeneric = typeof(IQueryProvider)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m is { Name: nameof(IQueryProvider.Execute), IsGenericMethod: true })
            .MakeGenericMethod(resultType);

        try
        {
            return (TResult)executeGeneric.Invoke(inMemoryQueryable.Provider, [rewrittenExpression])!;
        }
        catch (TargetInvocationException ex)
            when (ex.InnerException is ArgumentException { Message: var message }
                  && message.Contains("must be reducible node", StringComparison.OrdinalIgnoreCase)
                  && TryExecuteScalarFallback<TResult, TEntity>(expression, list, out var fallback))
        {
            return fallback;
        }
    }

    private static bool TryExecuteScalarFallback<TResult, TEntity>(Expression expression, IList<TEntity> list,
        out TResult result)
        where TEntity : class
    {
        result = default!;

        if (expression is not MethodCallExpression methodCall
            || methodCall.Method.DeclaringType != typeof(Queryable)
            || !TryEvaluateSourceSequence(methodCall.Arguments[0], list, out var source))
        {
            return false;
        }

        Func<TEntity, bool>? predicate = null;
        if (methodCall.Arguments.Count > 1)
        {
            var predicateLambda = ResolveLambda(methodCall.Arguments[1]);
            if (predicateLambda is null)
            {
                return false;
            }

            predicate = (Func<TEntity, bool>)predicateLambda.Compile();
        }

        object? scalar = methodCall.Method.Name switch
        {
            nameof(Queryable.FirstOrDefault) => predicate is null
                ? source.FirstOrDefault()
                : source.FirstOrDefault(predicate),
            nameof(Queryable.First) => predicate is null
                ? source.First()
                : source.First(predicate),
            nameof(Queryable.SingleOrDefault) => predicate is null
                ? source.SingleOrDefault()
                : source.SingleOrDefault(predicate),
            nameof(Queryable.Single) => predicate is null
                ? source.Single()
                : source.Single(predicate),
            nameof(Queryable.Any) => predicate is null
                ? source.Any()
                : source.Any(predicate),
            nameof(Queryable.Count) => predicate is null
                ? source.Count()
                : source.Count(predicate),
            nameof(Queryable.LongCount) => predicate is null
                ? source.LongCount()
                : source.LongCount(predicate),
            _ => null
        };

        if (scalar is null && typeof(TResult).IsValueType && Nullable.GetUnderlyingType(typeof(TResult)) is null)
        {
            return false;
        }

        result = (TResult)scalar!;
        return true;
    }

    private static bool TryEvaluateSourceSequence<TEntity>(Expression expression, IList<TEntity> root,
        out IEnumerable<TEntity> sequence)
        where TEntity : class
    {
        sequence = root;

        if (expression is not MethodCallExpression methodCall
            || methodCall.Method.DeclaringType != typeof(Queryable)
            || methodCall.Method.Name != nameof(Queryable.Where)
            || methodCall.Arguments.Count != 2)
        {
            return true;
        }

        if (!TryEvaluateSourceSequence(methodCall.Arguments[0], root, out var source))
        {
            return false;
        }

        var predicateLambda = ResolveLambda(methodCall.Arguments[1]);
        if (predicateLambda is null)
        {
            return false;
        }

        var predicate = (Func<TEntity, bool>)predicateLambda.Compile();
        sequence = source.Where(predicate);
        return true;
    }

    private static LambdaExpression? ResolveLambda(Expression expression)
    {
        var direct = UnwrapLambda(expression);
        if (direct is not null)
        {
            return direct;
        }

        if (expression is ConstantExpression { Value: LambdaExpression constantLambda })
        {
            return constantLambda;
        }

        if (expression.CanReduce)
        {
            return ResolveLambda(expression.Reduce());
        }

        try
        {
            var boxed = Expression.Convert(expression, typeof(object));
            var value = Expression.Lambda<Func<object?>>(boxed).Compile().Invoke();
            return value as LambdaExpression;
        }
        catch
        {
            return null;
        }
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

    private bool TryExtractPrimaryKeyConstraint<TEntity>(Expression expression, out PrimaryKeyConstraint keyConstraint)
        where TEntity : class
    {
        keyConstraint = PrimaryKeyConstraint.None;
        var predicate = ExtractPrimaryKeyPredicate(expression);
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

        if (!TryExtractPrimaryKeyConstraint(predicate.Body, primaryKeyName, out keyConstraint))
        {
            return false;
        }

        return true;
    }

    private static LambdaExpression? ExtractPrimaryKeyPredicate(Expression expression)
    {
        if (expression is not MethodCallExpression methodCall
            || methodCall.Method.DeclaringType != typeof(Queryable))
        {
            return null;
        }

        if (TryExtractPredicateFromMethod(methodCall, out var predicate))
        {
            return predicate;
        }

        if (methodCall.Arguments.Count == 0)
        {
            return null;
        }

        return ExtractPrimaryKeyPredicate(methodCall.Arguments[0]);
    }

    private static bool TryExtractPredicateFromMethod(MethodCallExpression methodCall, out LambdaExpression? predicate)
    {
        predicate = null;

        if (methodCall.Arguments.Count < 2)
        {
            return false;
        }

        if (methodCall.Method.Name is not (
            nameof(Queryable.Where)
            or nameof(Queryable.First)
            or nameof(Queryable.FirstOrDefault)
            or nameof(Queryable.Single)
            or nameof(Queryable.SingleOrDefault)
            or nameof(Queryable.Any)
            or nameof(Queryable.Count)
            or nameof(Queryable.LongCount)))
        {
            return false;
        }

        predicate = UnwrapLambda(methodCall.Arguments[1]);
        return predicate is not null;
    }

    private static LambdaExpression? UnwrapLambda(Expression expression)
    {
        if (expression is UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression quoted })
        {
            return quoted;
        }

        return expression as LambdaExpression;
    }

    private static bool TryExtractPrimaryKeyConstraint(Expression expression, string primaryKeyName,
        out PrimaryKeyConstraint keyConstraint)
    {
        keyConstraint = PrimaryKeyConstraint.None;

        if (expression is BinaryExpression { NodeType: ExpressionType.AndAlso } andAlso)
        {
            if (!TryExtractPrimaryKeyConstraint(andAlso.Left, primaryKeyName, out var left)
                || !TryExtractPrimaryKeyConstraint(andAlso.Right, primaryKeyName, out var right))
            {
                return false;
            }

            return TryMergePrimaryKeyConstraints(left, right, out keyConstraint);
        }

        if (expression is not BinaryExpression binary)
        {
            return false;
        }

        return TryExtractSingleComparisonConstraint(binary, primaryKeyName, out keyConstraint);
    }

    private static bool TryExtractSingleComparisonConstraint(BinaryExpression binary, string primaryKeyName,
        out PrimaryKeyConstraint keyConstraint)
    {
        keyConstraint = PrimaryKeyConstraint.None;

        if (!TryResolveMemberAndValue(binary.Left, binary.Right, out var memberName, out var rawValue))
        {
            if (!TryResolveMemberAndValue(binary.Right, binary.Left, out memberName, out rawValue))
            {
                return false;
            }

            var reversedNodeType = ReverseBinaryOperator(binary.NodeType);
            if (reversedNodeType is null)
            {
                return false;
            }

            binary = Expression.MakeBinary(reversedNodeType.Value, binary.Right, binary.Left);
        }

        if (!string.Equals(memberName, primaryKeyName, StringComparison.Ordinal)
            || rawValue is null)
        {
            return false;
        }

        switch (binary.NodeType)
        {
            case ExpressionType.Equal:
                keyConstraint = PrimaryKeyConstraint.ForEquality(rawValue);
                return true;
            case ExpressionType.GreaterThan:
                keyConstraint = PrimaryKeyConstraint.ForRange(rawValue, false, null, false);
                return true;
            case ExpressionType.GreaterThanOrEqual:
                keyConstraint = PrimaryKeyConstraint.ForRange(rawValue, true, null, false);
                return true;
            case ExpressionType.LessThan:
                keyConstraint = PrimaryKeyConstraint.ForRange(null, false, rawValue, false);
                return true;
            case ExpressionType.LessThanOrEqual:
                keyConstraint = PrimaryKeyConstraint.ForRange(null, false, rawValue, true);
                return true;
            default:
                return false;
        }
    }

    private static ExpressionType? ReverseBinaryOperator(ExpressionType expressionType)
    {
        return expressionType switch
        {
            ExpressionType.Equal => ExpressionType.Equal,
            ExpressionType.GreaterThan => ExpressionType.LessThan,
            ExpressionType.GreaterThanOrEqual => ExpressionType.LessThanOrEqual,
            ExpressionType.LessThan => ExpressionType.GreaterThan,
            ExpressionType.LessThanOrEqual => ExpressionType.GreaterThanOrEqual,
            _ => null
        };
    }

    private static bool TryMergePrimaryKeyConstraints(PrimaryKeyConstraint left, PrimaryKeyConstraint right,
        out PrimaryKeyConstraint merged)
    {
        merged = PrimaryKeyConstraint.None;

        if (left.IsNone || right.IsNone)
        {
            return false;
        }

        if (left.IsEquality && right.IsEquality)
        {
            if (!Equals(left.EqualityValue, right.EqualityValue))
            {
                return false;
            }

            merged = left;
            return true;
        }

        if (left.IsEquality)
        {
            return TryMergePrimaryKeyConstraints(right, left, out merged);
        }

        if (right.IsEquality)
        {
            var equality = right.EqualityValue;
            if (equality is null)
            {
                return false;
            }

            if (!IsEqualityWithinRange(left, equality))
            {
                return false;
            }

            merged = right;
            return true;
        }

        var (lowerBound, lowerInclusive) = SelectLowerBound(left, right);
        var (upperBound, upperInclusive) = SelectUpperBound(left, right);

        if (lowerBound is not null && upperBound is not null)
        {
            var compare = CompareComparable(lowerBound, upperBound);
            if (compare > 0 || (compare == 0 && (!lowerInclusive || !upperInclusive)))
            {
                return false;
            }
        }

        merged = PrimaryKeyConstraint.ForRange(lowerBound, lowerInclusive, upperBound, upperInclusive);
        return true;
    }

    private static (object? lowerBound, bool lowerInclusive) SelectLowerBound(PrimaryKeyConstraint left,
        PrimaryKeyConstraint right)
    {
        if (left.LowerBound is null)
        {
            return (right.LowerBound, right.LowerInclusive);
        }

        if (right.LowerBound is null)
        {
            return (left.LowerBound, left.LowerInclusive);
        }

        var compare = CompareComparable(left.LowerBound, right.LowerBound);
        if (compare > 0)
        {
            return (left.LowerBound, left.LowerInclusive);
        }

        if (compare < 0)
        {
            return (right.LowerBound, right.LowerInclusive);
        }

        return (left.LowerBound, left.LowerInclusive && right.LowerInclusive);
    }

    private static (object? upperBound, bool upperInclusive) SelectUpperBound(PrimaryKeyConstraint left,
        PrimaryKeyConstraint right)
    {
        if (left.UpperBound is null)
        {
            return (right.UpperBound, right.UpperInclusive);
        }

        if (right.UpperBound is null)
        {
            return (left.UpperBound, left.UpperInclusive);
        }

        var compare = CompareComparable(left.UpperBound, right.UpperBound);
        if (compare < 0)
        {
            return (left.UpperBound, left.UpperInclusive);
        }

        if (compare > 0)
        {
            return (right.UpperBound, right.UpperInclusive);
        }

        return (left.UpperBound, left.UpperInclusive && right.UpperInclusive);
    }

    private static bool IsEqualityWithinRange(PrimaryKeyConstraint rangeConstraint, object equality)
    {
        if (rangeConstraint.LowerBound is not null)
        {
            var lowerMatch = rangeConstraint.LowerInclusive
                ? EqualityComparer<object>.Default.Equals(equality, rangeConstraint.LowerBound)
                  || CompareComparable(equality, rangeConstraint.LowerBound) > 0
                : CompareComparable(equality, rangeConstraint.LowerBound) > 0;

            if (!lowerMatch)
            {
                return false;
            }
        }

        if (rangeConstraint.UpperBound is not null)
        {
            var upperMatch = rangeConstraint.UpperInclusive
                ? EqualityComparer<object>.Default.Equals(equality, rangeConstraint.UpperBound)
                  || CompareComparable(equality, rangeConstraint.UpperBound) < 0
                : CompareComparable(equality, rangeConstraint.UpperBound) < 0;

            if (!upperMatch)
            {
                return false;
            }
        }

        return true;
    }

    private static int CompareComparable(object left, object right)
    {
        if (left is IComparable comparable)
        {
            return comparable.CompareTo(right);
        }

        throw new InvalidOperationException("Primary key value does not implement IComparable.");
    }

    private static bool TryResolveMemberAndValue(Expression memberCandidate, Expression valueCandidate,
        out string memberName, out object? value)
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

    private sealed class ExtensionReducingVisitor : ExpressionVisitor
    {
        protected override Expression VisitExtension(Expression node)
        {
            return node.CanReduce
                ? Visit(node.Reduce())
                : base.VisitExtension(node);
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

    private readonly record struct PrimaryKeyConstraint(
        bool IsEquality,
        object? EqualityValue,
        object? LowerBound,
        bool LowerInclusive,
        object? UpperBound,
        bool UpperInclusive)
    {
        public static PrimaryKeyConstraint None => new(false, null, null, false, null, false);
        public bool IsNone => !IsEquality && LowerBound is null && UpperBound is null;
        public bool HasRangeBound => LowerBound is not null || UpperBound is not null;

        public static PrimaryKeyConstraint ForEquality(object equalityValue)
        {
            return new PrimaryKeyConstraint(true, equalityValue, null, false, null, false);
        }

        public static PrimaryKeyConstraint ForRange(object? lowerBound, bool lowerInclusive, object? upperBound,
            bool upperInclusive)
        {
            return new PrimaryKeyConstraint(false, null, lowerBound, lowerInclusive, upperBound, upperInclusive);
        }
    }
}