namespace CloudStorageORM.Infrastructure
{
    using Microsoft.EntityFrameworkCore.Query;
    using System.Linq.Expressions;
    using Microsoft.EntityFrameworkCore;

    internal class InMemoryExpressionVisitor : ExpressionVisitor
    {
        private readonly QueryContext _queryContext;

        public InMemoryExpressionVisitor(QueryContext queryContext)
        {
            _queryContext = queryContext;
        }

        protected override Expression VisitExtension(Expression node)
        {
            if (node is QueryRootExpression queryRoot)
            {
                var entityClrType = queryRoot.Type.GetGenericArguments().First();

                var providerType = typeof(CloudStorageQueryProvider);
                var queryableType = typeof(CloudStorageQueryable<>).MakeGenericType(entityClrType);

                var queryProvider = (IQueryProvider)Activator.CreateInstance(providerType, _queryContext.Context)!;
                var queryable = (IQueryable)Activator.CreateInstance(queryableType, queryProvider)!;

                return queryable.Expression;
            }

            return base.VisitExtension(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            // If it's already a constant DbSet, DO NOT visit it again
            if (node.Value is IQueryable)
            {
                return node;
            }

            return base.VisitConstant(node);
        }
    }
}
