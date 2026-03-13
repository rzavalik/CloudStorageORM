using System.Linq.Expressions;
using CloudStorageORM.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

/// <summary>Tests for the internal InMemoryExpressionVisitor.</summary>
public class InMemoryExpressionVisitorTests
{
    private static readonly int[] SourceArray = [1, 2, 3];

    [Fact]
    public void VisitConstant_WithIQueryable_ReturnsNodeUnchanged()
    {
        var visitor = new InMemoryExpressionVisitor();

        var queryable = SourceArray.AsQueryable();
        var node = Expression.Constant(queryable);

        // VisitConstant with an IQueryable returns the node unchanged
        var result = visitor.Visit(node);

        result.ShouldBeSameAs(node);
    }

    [Fact]
    public void VisitConstant_WithNonQueryableConstant_ReturnsNodeUnchanged()
    {
        var visitor = new InMemoryExpressionVisitor();

        var node = Expression.Constant("hello");
        var result = visitor.Visit(node);

        result.ShouldBeSameAs(node);
    }

    [Fact]
    public void VisitExtension_WithQueryRootExpression_RewritesToInMemoryQueryableExpression()
    {
        var options = new DbContextOptionsBuilder<VisitorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new VisitorDbContext(options);
        var visitor = new InMemoryExpressionVisitor();
        var queryRoot = ((IQueryable)context.Entities).Expression;

        var result = visitor.Visit(queryRoot);

        result.ShouldBeOfType<ConstantExpression>();
        result.Type.ShouldBe(typeof(EnumerableQuery<VisitorEntity>));
        ((ConstantExpression)result).Value.ShouldBeAssignableTo<IQueryable<VisitorEntity>>();
    }

    [Fact]
    public void VisitExtension_WithNonQueryRootExtension_UsesBaseBehavior()
    {
        var visitor = new InMemoryExpressionVisitor();
        var extensionNode = new PassthroughExtensionExpression();

        var result = visitor.Visit(extensionNode);

        result.ShouldBeOfType<ConstantExpression>();
        ((ConstantExpression)result).Value.ShouldBe(42);
    }

    private sealed class VisitorDbContext(DbContextOptions<VisitorDbContext> options) : DbContext(options)
    {
        public DbSet<VisitorEntity> Entities => Set<VisitorEntity>();
    }

    private sealed class VisitorEntity
    {
        public int Id { get; init; }
    }

    private sealed class PassthroughExtensionExpression : Expression
    {
        public override Type Type => typeof(int);

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override bool CanReduce => true;

        public override Expression Reduce()
        {
            return Constant(42);
        }
    }
}