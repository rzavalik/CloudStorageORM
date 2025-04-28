namespace CloudStorageORM.Infrastructure
{
    using Microsoft.EntityFrameworkCore.Query;
    using System.Linq.Expressions;

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