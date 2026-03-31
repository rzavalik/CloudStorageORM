using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace CloudStorageORM.Infrastructure;

public class CloudStorageQueryVisitor : ExpressionVisitor
{
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