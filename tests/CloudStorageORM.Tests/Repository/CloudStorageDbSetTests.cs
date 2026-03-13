namespace CloudStorageORM.Tests.Repository
{
    using System.Collections;
    using System.Linq.Expressions;
    using CloudStorageORM.Repositories;
    using Interfaces.StorageProviders;
    using Moq;
    using Shouldly;

    public class CloudStorageDbSetTests
    {
        [Fact]
        public async Task ToListAsync_LoadsAndCachesEntities()
        {
            var provider = new Mock<IStorageProvider>();
            provider.Setup(x => x.ListAsync("DbSetUser"))
                .ReturnsAsync(["DbSetUser/1.json", "DbSetUser/2.json"]);
            provider.Setup(x => x.ReadAsync<DbSetUser>("DbSetUser/1.json"))
                .ReturnsAsync(new DbSetUser { Id = "1", Name = "A" });
            provider.Setup(x => x.ReadAsync<DbSetUser>("DbSetUser/2.json"))
                .ReturnsAsync(new DbSetUser { Id = "2", Name = "B" });

            var sut = new CloudStorageDbSet<DbSetUser>(provider.Object);

            var first = await sut.ToListAsync();
            var second = await sut.ToListAsync();

            first.Count.ShouldBe(2);
            second.Count.ShouldBe(2);
            provider.Verify(x => x.ListAsync("DbSetUser"), Times.Once);
        }

        [Fact]
        public async Task FirstOrDefaultAsync_ReturnsMatch()
        {
            var provider = new Mock<IStorageProvider>();
            provider.Setup(x => x.ListAsync("DbSetUser"))
                .ReturnsAsync(["DbSetUser/1.json"]);
            provider.Setup(x => x.ReadAsync<DbSetUser>("DbSetUser/1.json"))
                .ReturnsAsync(new DbSetUser { Id = "1", Name = "A" });

            var sut = new CloudStorageDbSet<DbSetUser>(provider.Object);
            var result = await sut.FirstOrDefaultAsync(x => x.Id == "1");

            result.ShouldNotBeNull();
            result.Name.ShouldBe("A");
        }

        [Fact]
        public async Task FirstOrDefaultAsync_WhenNoMatch_ReturnsNull()
        {
            var provider = new Mock<IStorageProvider>();
            provider.Setup(x => x.ListAsync("DbSetUser"))
                .ReturnsAsync(["DbSetUser/1.json"]);
            provider.Setup(x => x.ReadAsync<DbSetUser>("DbSetUser/1.json"))
                .ReturnsAsync(new DbSetUser { Id = "1", Name = "A" });

            var sut = new CloudStorageDbSet<DbSetUser>(provider.Object);
            var result = await sut.FirstOrDefaultAsync(x => x.Id == "missing");

            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetAsyncEnumerator_YieldsAllItems()
        {
            var provider = new Mock<IStorageProvider>();
            provider.Setup(x => x.ListAsync("DbSetUser"))
                .ReturnsAsync(["DbSetUser/1.json"]);
            provider.Setup(x => x.ReadAsync<DbSetUser>("DbSetUser/1.json"))
                .ReturnsAsync(new DbSetUser { Id = "1", Name = "A" });

            var sut = new CloudStorageDbSet<DbSetUser>(provider.Object);
            var list = new List<DbSetUser>();
            await foreach (var item in sut)
            {
                list.Add(item);
            }

            list.Count.ShouldBe(1);
            list[0].Id.ShouldBe("1");
        }

        [Fact]
        public void GetEnumerator_WithoutCache_ReturnsEmpty()
        {
            var sut = new CloudStorageDbSet<DbSetUser>(new Mock<IStorageProvider>().Object);
            sut.ToList().ShouldBeEmpty();
        }

        [Fact]
        public async Task GetEnumerator_WithCache_ReturnsCachedItems()
        {
            var provider = new Mock<IStorageProvider>();
            provider.Setup(x => x.ListAsync("DbSetUser"))
                .ReturnsAsync(["DbSetUser/1.json"]);
            provider.Setup(x => x.ReadAsync<DbSetUser>("DbSetUser/1.json"))
                .ReturnsAsync(new DbSetUser { Id = "1", Name = "A" });

            var sut = new CloudStorageDbSet<DbSetUser>(provider.Object);
            await sut.ToListAsync();

            var items = sut.ToList();

            items.Count.ShouldBe(1);
            items[0].Id.ShouldBe("1");
        }

        [Fact]
        public async Task IEnumerable_GetEnumerator_DelegatesToGenericEnumerator()
        {
            var provider = new Mock<IStorageProvider>();
            provider.Setup(x => x.ListAsync("DbSetUser"))
                .ReturnsAsync(["DbSetUser/1.json"]);
            provider.Setup(x => x.ReadAsync<DbSetUser>("DbSetUser/1.json"))
                .ReturnsAsync(new DbSetUser { Id = "1", Name = "A" });

            var sut = new CloudStorageDbSet<DbSetUser>(provider.Object);
            await sut.ToListAsync();

            IEnumerable nonGeneric = sut;
            var iterator = nonGeneric.GetEnumerator();
            using var disposable = iterator as IDisposable;

            iterator.MoveNext().ShouldBeTrue();
            ((DbSetUser?)iterator.Current)?.Id.ShouldBe("1");
            iterator.MoveNext().ShouldBeFalse();
        }

        [Fact]
        public void ElementType_ReturnsEntityType()
        {
            var sut = new CloudStorageDbSet<DbSetUser>(new Mock<IStorageProvider>().Object);

            sut.ElementType.ShouldBe(typeof(DbSetUser));
        }

        [Fact]
        public void Expression_ReturnsConstantExpressionForInstance()
        {
            var sut = new CloudStorageDbSet<DbSetUser>(new Mock<IStorageProvider>().Object);

            sut.Expression.ShouldBeOfType<ConstantExpression>();
            ((ConstantExpression)sut.Expression).Value.ShouldBe(sut);
        }

        [Fact]
        public void Provider_IsQueryableProvider()
        {
            var sut = new CloudStorageDbSet<DbSetUser>(new Mock<IStorageProvider>().Object);

            sut.Provider.ShouldNotBeNull();
        }

        private sealed class DbSetUser
        {
            public string Id { get; init; } = string.Empty;
            public string Name { get; init; } = string.Empty;
        }
    }
}