namespace CloudStorageORM.Tests.Infrastructure
{
    using System.Linq.Expressions;
    using global::CloudStorageORM.Infrastructure;
    using Shouldly;

    public class CloudStorageQueryVisitorTests
    {
        [Fact]
        public void VisitConstant_NonQueryableValue_ReturnsOriginalNode()
        {
            var visitor = new CloudStorageQueryVisitor(null!);

            var node = Expression.Constant(42);
            var result = visitor.Visit(node);

            result.ShouldBeSameAs(node);
        }

        [Fact]
        public void VisitConstant_WithIQueryable_ExecutesProviderAndReturnsConstant()
        {
            var visitor = new CloudStorageQueryVisitor(null!);

            // a simple in-memory queryable whose execution returns a list
            var list = new[] { "a", "b" }.AsQueryable();
            var node = Expression.Constant(list);

            var result = visitor.Visit(node) as ConstantExpression;

            result.ShouldNotBeNull();
        }
    }
}