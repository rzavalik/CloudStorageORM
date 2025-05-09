namespace CloudStorageORM.Infrastructure
{
    using System.Linq.Expressions;
    using Microsoft.EntityFrameworkCore.Query;

    public class CloudStorageQueryVisitor : ExpressionVisitor
    {
        private readonly QueryContext _context;

        public CloudStorageQueryVisitor(QueryContext context)
        {
            _context = context;
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
}