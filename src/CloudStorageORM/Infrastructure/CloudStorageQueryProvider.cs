using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Observability;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// Query provider that executes LINQ expressions against object-storage-backed entities.
/// </summary>
public class CloudStorageQueryProvider(
    CloudStorageDatabase database,
    IBlobPathResolver blobPathResolver,
    ILogger<CloudStorageQueryProvider>? logger = null,
    bool enableLogging = true,
    bool enableTracing = true)
    : IAsyncQueryProvider
{
    private readonly CloudStorageDatabase _database = database ?? throw new ArgumentNullException(nameof(database));

    private readonly IBlobPathResolver _blobPathResolver =
        blobPathResolver ?? throw new ArgumentNullException(nameof(blobPathResolver));

    private readonly ILogger<CloudStorageQueryProvider>? _logger = logger;
    private readonly bool _enableLogging = enableLogging;
    private readonly bool _enableTracing = enableTracing;

    private Task<IList<T>> LoadEntitiesAsync<T>() where T : class
    {
        return _database.ToListAsync<T>(
            _blobPathResolver.GetBlobName(typeof(T))
        );
    }

    /// <inheritdoc />
    public IQueryable CreateQuery(Expression expression)
    {
        var elementType = expression.Type.GetGenericArguments().First();
        var queryableType = typeof(CloudStorageQueryable<>).MakeGenericType(elementType);
        return (IQueryable)Activator.CreateInstance(queryableType, this, expression)!;
    }

    /// <inheritdoc />
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new CloudStorageQueryable<TElement>(this, expression);
    }

    /// <inheritdoc />
    public object Execute(Expression expression)
    {
        return ExecuteCoreAsync<object>(expression, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public TResult Execute<TResult>(Expression expression)
    {
        return ExecuteCoreAsync<TResult>(expression, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes a LINQ expression asynchronously and returns the materialized result.
    /// </summary>
    /// <typeparam name="TResult">Expected query result type.</typeparam>
    /// <param name="expression">Expression tree to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The query result.</returns>
    /// <example>
    /// <code>
    /// var result = await provider.ExecuteAsync&lt;IEnumerable&lt;User&gt;&gt;(query.Expression);
    /// </code>
    /// </example>
    public async Task<TResult> ExecuteAsync<TResult>(Expression expression,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteCoreAsync<TResult>(expression, cancellationToken);
    }

    TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        return ExecuteCoreAsync<TResult>(expression, cancellationToken).GetAwaiter().GetResult();
    }

    private async Task<TResult> ExecuteCoreAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        var elementType = ResolveEntityType(expression, typeof(TResult));
        var executeMethod = typeof(CloudStorageQueryProvider)
            .GetMethod(nameof(ExecuteTypedAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(elementType, typeof(TResult));

        return await _database.ExecuteWithRetryAsync(async token =>
        {
            var task = (Task)executeMethod.Invoke(this, [expression, token])!;
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result")!;
            return (TResult)resultProperty.GetValue(task)!;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResult> ExecuteTypedAsync<TEntity, TResult>(Expression expression,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var activity = _enableTracing ? CloudStorageOrmActivitySource.StartActivity("Query") : null;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (_enableLogging)
            {
                _logger?.LogQueryExecutionStarting(typeof(TEntity).Name);
            }

            if (TryExecuteSkipTakePushdown<TEntity, TResult>(expression, out var pagedResult))
            {
                stopwatch.Stop();
                return await pagedResult.ConfigureAwait(false);
            }

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
            var result = ExecuteAgainstInMemory<TResult, TEntity>(expression, list);

            stopwatch.Stop();

            var resultCount = 0;
            if (result is System.Collections.IEnumerable enumerable && result is not string)
            {
                resultCount = enumerable.Cast<object>().Count();
            }

            if (_enableLogging)
            {
                _logger?.LogQueryExecutionCompleted(typeof(TEntity).Name, resultCount, stopwatch.ElapsedMilliseconds);
            }

            activity?.SetTag("query.result_count", resultCount);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            if (_enableLogging)
            {
                _logger?.LogQueryExecutionFailed(typeof(TEntity).Name, ex, stopwatch.ElapsedMilliseconds);
            }

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private bool TryExecuteSkipTakePushdown<TEntity, TResult>(Expression expression, out Task<TResult> resultTask)
        where TEntity : class
    {
        resultTask = Task.FromResult(default(TResult)!);

        if (!TryExtractSkipTakeShape(expression, out var shape)
            || shape.Take is null
            || shape.Take.Value < 0
            || shape.Skip < 0
            || ContainsUnsupportedPaginationOperators(shape.BaseExpression)
            || !IsEnumerableResult(typeof(TResult)))
        {
            return false;
        }

        var hasPredicate = ExtractPrimaryKeyPredicate(shape.BaseExpression) is not null;
        var hasPrimaryKeyConstraint =
            TryExtractPrimaryKeyConstraint<TEntity>(shape.BaseExpression, out var keyConstraint);

        if (hasPredicate && !hasPrimaryKeyConstraint)
        {
            return false;
        }

        resultTask =
            ExecuteSkipTakePushdownAsync<TEntity, TResult>(shape, hasPrimaryKeyConstraint ? keyConstraint : null);
        return true;
    }

    private async Task<TResult> ExecuteSkipTakePushdownAsync<TEntity, TResult>(
        SkipTakeShape shape,
        PrimaryKeyConstraint? keyConstraint)
        where TEntity : class
    {
        var take = shape.Take ?? 0;
        if (take == 0)
        {
            return ExecuteAgainstInMemory<TResult, TEntity>(shape.BaseExpression, []);
        }

        IList<TEntity> page;

        switch (keyConstraint)
        {
            case { IsEquality: true, EqualityValue: not null }:
            {
                var entity = await _database.TryLoadByPrimaryKeyAsync<TEntity>(keyConstraint.Value.EqualityValue!)
                    .ConfigureAwait(false);
                var equalityList = entity is null
                    ? new List<TEntity>()
                    : new List<TEntity> { entity };
                page = equalityList.Skip(shape.Skip).Take(take).ToList();
                break;
            }

            case { IsEquality: false, HasRangeBound: true }:
                page = await _database
                    .LoadByPrimaryKeyRangePageAsync<TEntity>(
                        keyConstraint.Value.LowerBound,
                        keyConstraint.Value.LowerInclusive,
                        keyConstraint.Value.UpperBound,
                        keyConstraint.Value.UpperInclusive,
                        shape.Skip,
                        take)
                    .ConfigureAwait(false);
                break;

            default:
                page = await _database.LoadPageAsync<TEntity>(shape.Skip, take).ConfigureAwait(false);
                break;
        }

        return ExecuteAgainstInMemory<TResult, TEntity>(shape.BaseExpression, page);
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
        if (IsEnumerableResult(resultType) || resultType == typeof(object) &&
            typeof(IEnumerable<TEntity>).IsAssignableFrom(rewrittenExpression.Type))
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

    private static bool TryExtractSkipTakeShape(Expression expression, out SkipTakeShape shape)
    {
        shape = new SkipTakeShape(0, null, expression);
        var skip = 0;
        int? take = null;
        var foundPagination = false;
        var current = expression;

        while (current is MethodCallExpression methodCall
               && methodCall.Method.DeclaringType == typeof(Queryable)
               && methodCall.Arguments.Count >= 2
               && methodCall.Method.Name is nameof(Queryable.Skip) or nameof(Queryable.Take))
        {
            if (!TryEvaluatePaginationCount(methodCall.Arguments[1], out var count))
            {
                return false;
            }

            if (count < 0)
            {
                return false;
            }

            if (methodCall.Method.Name == nameof(Queryable.Skip))
            {
                skip += count;
            }
            else
            {
                take = take.HasValue ? Math.Min(take.Value, count) : count;
            }

            foundPagination = true;
            current = methodCall.Arguments[0];
        }

        if (!foundPagination)
        {
            return false;
        }

        shape = new SkipTakeShape(skip, take, current);
        return true;
    }

    private static bool TryEvaluatePaginationCount(Expression expression, out int count)
    {
        count = 0;

        try
        {
            var boxed = Expression.Convert(expression, typeof(int));
            count = Expression.Lambda<Func<int>>(boxed).Compile().Invoke();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ContainsUnsupportedPaginationOperators(Expression expression)
    {
        var visitor = new UnsupportedPaginationOperatorVisitor();
        visitor.Visit(expression);
        return visitor.HasUnsupportedOperator;
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
        while (true)
        {
            if (expression is not MethodCallExpression methodCall ||
                methodCall.Method.DeclaringType != typeof(Queryable))
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

            expression = methodCall.Arguments[0];
        }
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

        if (expression is not BinaryExpression { NodeType: ExpressionType.AndAlso } andAlso)
        {
            return expression is BinaryExpression binary
                   && TryExtractSingleComparisonConstraint(binary, primaryKeyName, out keyConstraint);
        }

        if (!TryExtractPrimaryKeyConstraint(andAlso.Left, primaryKeyName, out var left)
            || !TryExtractPrimaryKeyConstraint(andAlso.Right, primaryKeyName, out var right))
        {
            return false;
        }

        return TryMergePrimaryKeyConstraints(left, right, out keyConstraint);
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
        while (true)
        {
            merged = PrimaryKeyConstraint.None;

            if (left.IsNone || right.IsNone)
            {
                return false;
            }

            switch (left.IsEquality)
            {
                case true when right.IsEquality:
                {
                    if (!Equals(left.EqualityValue, right.EqualityValue))
                    {
                        return false;
                    }

                    merged = left;
                    return true;
                }
                case true:
                {
                    (left, right) = (right, left);
                    continue;
                }
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
        return compare switch
        {
            > 0 => (left.LowerBound, left.LowerInclusive),
            < 0 => (right.LowerBound, right.LowerInclusive),
            _ => (left.LowerBound, left.LowerInclusive && right.LowerInclusive)
        };
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
        return compare switch
        {
            < 0 => (left.UpperBound, left.UpperInclusive),
            > 0 => (right.UpperBound, right.UpperInclusive),
            _ => (left.UpperBound, left.UpperInclusive && right.UpperInclusive)
        };
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

        if (rangeConstraint.UpperBound is null)
        {
            return true;
        }

        var upperMatch = rangeConstraint.UpperInclusive
            ? EqualityComparer<object>.Default.Equals(equality, rangeConstraint.UpperBound)
              || CompareComparable(equality, rangeConstraint.UpperBound) < 0
            : CompareComparable(equality, rangeConstraint.UpperBound) < 0;

        return upperMatch;
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

    private sealed class UnsupportedPaginationOperatorVisitor : ExpressionVisitor
    {
        public bool HasUnsupportedOperator { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (HasUnsupportedOperator)
            {
                return node;
            }

            if (node.Method.DeclaringType != typeof(Queryable)
                || node.Method.Name is not (nameof(Queryable.OrderBy)
                    or nameof(Queryable.OrderByDescending)
                    or nameof(Queryable.ThenBy)
                    or nameof(Queryable.ThenByDescending)
                    or nameof(Queryable.Select)
                    or nameof(Queryable.SelectMany)
                    or nameof(Queryable.Reverse)
                    or nameof(Queryable.GroupBy)
                    or nameof(Queryable.Distinct)))
            {
                return base.VisitMethodCall(node);
            }

            HasUnsupportedOperator = true;
            return node;
        }
    }

    private readonly record struct SkipTakeShape(int Skip, int? Take, Expression BaseExpression);

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

        /// <summary>
        /// Creates an equality-based primary-key constraint.
        /// </summary>
        /// <param name="equalityValue">Primary-key value that must match exactly.</param>
        /// <returns>A constraint representing primary-key equality.</returns>
        public static PrimaryKeyConstraint ForEquality(object equalityValue)
        {
            return new PrimaryKeyConstraint(true, equalityValue, null, false, null, false);
        }

        /// <summary>
        /// Creates a range-based primary-key constraint.
        /// </summary>
        /// <param name="lowerBound">Optional lower bound value.</param>
        /// <param name="lowerInclusive"><see langword="true" /> when the lower bound is inclusive.</param>
        /// <param name="upperBound">Optional upper bound value.</param>
        /// <param name="upperInclusive"><see langword="true" /> when the upper bound is inclusive.</param>
        /// <returns>A constraint representing a primary-key range.</returns>
        public static PrimaryKeyConstraint ForRange(object? lowerBound, bool lowerInclusive, object? upperBound,
            bool upperInclusive)
        {
            return new PrimaryKeyConstraint(false, null, lowerBound, lowerInclusive, upperBound, upperInclusive);
        }
    }
}