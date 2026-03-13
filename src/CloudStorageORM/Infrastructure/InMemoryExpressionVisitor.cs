namespace CloudStorageORM.Infrastructure
{
    using System.Linq.Expressions;
    using Microsoft.EntityFrameworkCore.Query;

    public class InMemoryExpressionVisitor : ExpressionVisitor
    {
        protected override Expression VisitExtension(Expression node)
        {
            if (node is not QueryRootExpression queryRoot)
            {
                return base.VisitExtension(node);
            }

            var entityClrType = queryRoot.Type.GetGenericArguments().First();

            return CreateEmptyQueryableExpression(entityClrType);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            // If it's already a constant DbSet, DO NOT visit it again
            return node.Value is IQueryable ? node : base.VisitConstant(node);
        }

        private static Expression CreateEmptyQueryableExpression(Type entityClrType)
        {
            var queryableType = typeof(EnumerableQuery<>).MakeGenericType(entityClrType);
            var emptyArray = Array.CreateInstance(entityClrType, 0);
            var queryable = (IQueryable)Activator.CreateInstance(queryableType, emptyArray)!;

            return queryable.Expression;
        }
    }
}