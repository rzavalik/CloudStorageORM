using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// Expression visitor used to evaluate provider-backed query constants.
/// </summary>
public class CloudStorageQueryVisitor : ExpressionVisitor
{
    /// <summary>
    /// Creates a query visitor for a specific EF query context.
    /// </summary>
    /// <param name="context">Query context instance associated with the current query execution.</param>
    public CloudStorageQueryVisitor(QueryContext context)
    {
        _ = context;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value is IQueryable queryable)
        {
            return Expression.Constant(queryable.Provider.Execute(Expression.Constant(queryable.Expression)));
        }

        return base.VisitConstant(node);
    }
}