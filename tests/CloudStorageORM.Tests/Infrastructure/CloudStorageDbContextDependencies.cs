namespace CloudStorageORM.Tests.Infrastructure
{
    using global::CloudStorageORM.Infrastructure;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.EntityFrameworkCore.Query;
    using Microsoft.EntityFrameworkCore.Storage;
    using Moq;
    using Shouldly;
    using Xunit;

    public class CloudStorageDbContextDependenciesTests
    {
        private CloudStorageDbContextDependencies MakeSut(
            IModel model,
            ICurrentDbContext currentContext,
            IChangeDetector changeDetector,
            IDbSetSource setSource,
            IEntityFinderSource entityFinderSource,
            IEntityGraphAttacher entityGraphAttacher,
            IAsyncQueryProvider queryProvider,
            IStateManager stateManager,
            IExceptionDetector exceptionDetector,
            IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger,
            IDiagnosticsLogger<DbLoggerCategory.Infrastructure> infrastructureLogger)
        {
            return new CloudStorageDbContextDependencies(
                model,
                currentContext,
                changeDetector,
                setSource,
                entityFinderSource,
                entityGraphAttacher,
                queryProvider,
                stateManager,
                exceptionDetector,
                updateLogger,
                infrastructureLogger
            );
        }

        private (
            IModel,
            ICurrentDbContext,
            IChangeDetector,
            IDbSetSource,
            IEntityFinderSource,
            IEntityGraphAttacher,
            IAsyncQueryProvider,
            IStateManager,
            IExceptionDetector,
            IDiagnosticsLogger<DbLoggerCategory.Update>,
            IDiagnosticsLogger<DbLoggerCategory.Infrastructure>) CreateValidDependencies()
        {
            return (
                new Mock<IModel>().Object,
                new Mock<ICurrentDbContext>().Object,
                new Mock<IChangeDetector>().Object,
                new Mock<IDbSetSource>().Object,
                new Mock<IEntityFinderSource>().Object,
                new Mock<IEntityGraphAttacher>().Object,
                new Mock<IAsyncQueryProvider>().Object,
                new Mock<IStateManager>().Object,
                new Mock<IExceptionDetector>().Object,
                new Mock<IDiagnosticsLogger<DbLoggerCategory.Update>>().Object,
                new Mock<IDiagnosticsLogger<DbLoggerCategory.Infrastructure>>().Object
            );
        }

        [Fact]
        public void Constructor_WithNullModel_ThrowsArgumentNullException()
        {
            var dependencies = CreateValidDependencies();

            var exception = Should.Throw<ArgumentNullException>(() =>
                MakeSut(
                    null,
                    dependencies.Item2,
                    dependencies.Item3,
                    dependencies.Item4,
                    dependencies.Item5,
                    dependencies.Item6,
                    dependencies.Item7,
                    dependencies.Item8,
                    dependencies.Item9,
                    dependencies.Item10,
                    dependencies.Item11
                )
            );

            exception.ParamName.ShouldBe("model");
        }

        [Fact]
        public void Constructor_WithNullCurrentContext_ThrowsArgumentNullException()
        {
            var dependencies = CreateValidDependencies();

            var exception = Should.Throw<ArgumentNullException>(() =>
                MakeSut(
                    dependencies.Item1,
                    null,
                    dependencies.Item3,
                    dependencies.Item4,
                    dependencies.Item5,
                    dependencies.Item6,
                    dependencies.Item7,
                    dependencies.Item8,
                    dependencies.Item9,
                    dependencies.Item10,
                    dependencies.Item11
                )
            );

            exception.ParamName.ShouldBe("currentContext");
        }

        [Fact]
        public void Constructor_WithNullChangeDetector_ThrowsArgumentNullException()
        {
            var dependencies = CreateValidDependencies();

            var exception = Should.Throw<ArgumentNullException>(() =>
                MakeSut(
                    dependencies.Item1,
                    dependencies.Item2,
                    null,
                    dependencies.Item4,
                    dependencies.Item5,
                    dependencies.Item6,
                    dependencies.Item7,
                    dependencies.Item8,
                    dependencies.Item9,
                    dependencies.Item10,
                    dependencies.Item11
                )
            );

            exception.ParamName.ShouldBe("changeDetector");
        }

        [Fact]
        public void Constructor_WithNullSetSource_ThrowsArgumentNullException()
        {
            var dependencies = CreateValidDependencies();

            var exception = Should.Throw<ArgumentNullException>(() =>
                MakeSut(
                    dependencies.Item1,
                    dependencies.Item2,
                    dependencies.Item3,
                    null,
                    dependencies.Item5,
                    dependencies.Item6,
                    dependencies.Item7,
                    dependencies.Item8,
                    dependencies.Item9,
                    dependencies.Item10,
                    dependencies.Item11
                )
            );

            exception.ParamName.ShouldBe("setSource");
        }

        [Fact]
        public void Constructor_WithNullEntityFinderSource_ThrowsArgumentNullException()
        {
            var dependencies = CreateValidDependencies();

            var exception = Should.Throw<ArgumentNullException>(() =>
                MakeSut(
                    dependencies.Item1,
                    dependencies.Item2,
                    dependencies.Item3,
                    dependencies.Item4,
                    null,
                    dependencies.Item6,
                    dependencies.Item7,
                    dependencies.Item8,
                    dependencies.Item9,
                    dependencies.Item10,
                    dependencies.Item11
                )
            );

            exception.ParamName.ShouldBe("entityFinderSource");
        }

        [Fact]
        public void Constructor_WithNullEntityGraphAttacher_ThrowsArgumentNullException()
        {
            var dependencies = CreateValidDependencies();

            var exception = Should.Throw<ArgumentNullException>(() =>
                MakeSut(
                    dependencies.Item1,
                    dependencies.Item2,
                    dependencies.Item3,
                    dependencies.Item4,
                    dependencies.Item5,
                    null,
                    dependencies.Item7,
                    dependencies.Item8,
                    dependencies.Item9,
                    dependencies.Item10,
                    dependencies.Item11
                )
            );

            exception.ParamName.ShouldBe("entityGraphAttacher");
        }

        [Fact]
        public void Constructor_WithNullQueryProvider_ThrowsArgumentNullException()
        {
            var dependencies = CreateValidDependencies();

            var exception = Should.Throw<ArgumentNullException>(() =>
                MakeSut(
                    dependencies.Item1,
                    dependencies.Item2,
                    dependencies.Item3,
                    dependencies.Item4,
                    dependencies.Item5,
                    dependencies.Item6,
                    null,
                    dependencies.Item8,
                    dependencies.Item9,
                    dependencies.Item10,
                    dependencies.Item11
                )
            );

            exception.ParamName.ShouldBe("queryProvider");
        }

        [Fact]
        public void Constructor_WithNullStateManager_ThrowsArgumentNullException()
        {
            var dependencies = CreateValidDependencies();

            var exception = Should.Throw<ArgumentNullException>(() =>
                MakeSut(
                    dependencies.Item1,
                    dependencies.Item2,
                    dependencies.Item3,
                    dependencies.Item4,
                    dependencies.Item5,
                    dependencies.Item6,
                    dependencies.Item7,
                    null,
                    dependencies.Item9,
                    dependencies.Item10,
                    dependencies.Item11
                )
            );

            exception.ParamName.ShouldBe("stateManager");
        }

        [Fact]
        public void Constructor_WithNullExceptionDetector_ThrowsArgumentNullException()
        {
            var dependencies = CreateValidDependencies();

            var exception = Should.Throw<ArgumentNullException>(() =>
                MakeSut(
                    dependencies.Item1,
                    dependencies.Item2,
                    dependencies.Item3,
                    dependencies.Item4,
                    dependencies.Item5,
                    dependencies.Item6,
                    dependencies.Item7,
                    dependencies.Item8,
                    null,
                    dependencies.Item10,
                    dependencies.Item11
                )
            );

            exception.ParamName.ShouldBe("exceptionDetector");
        }

        [Fact]
        public void Constructor_WithNullUpdateLogger_ThrowsArgumentNullException()
        {
            var dependencies = CreateValidDependencies();

            var exception = Should.Throw<ArgumentNullException>(() =>
                MakeSut(
                    dependencies.Item1,
                    dependencies.Item2,
                    dependencies.Item3,
                    dependencies.Item4,
                    dependencies.Item5,
                    dependencies.Item6,
                    dependencies.Item7,
                    dependencies.Item8,
                    dependencies.Item9,
                    null,
                    dependencies.Item11
                )
            );

            exception.ParamName.ShouldBe("updateLogger");
        }

        [Fact]
        public void Constructor_WithNullInfrastructureLogger_ThrowsArgumentNullException()
        {
            var dependencies = CreateValidDependencies();

            var exception = Should.Throw<ArgumentNullException>(() =>
                MakeSut(
                    dependencies.Item1,
                    dependencies.Item2,
                    dependencies.Item3,
                    dependencies.Item4,
                    dependencies.Item5,
                    dependencies.Item6,
                    dependencies.Item7,
                    dependencies.Item8,
                    dependencies.Item9,
                    dependencies.Item10,
                    null
                )
            );

            exception.ParamName.ShouldBe("infrastructureLogger");
        }

        [Fact]
        public void Constructor_WithValidDependencies_SetsAllPropertiesCorrectly()
        {
            var modelMock = new Mock<IModel>().Object;
            var currentContextMock = new Mock<ICurrentDbContext>();
            var changeDetectorMock = new Mock<IChangeDetector>().Object;
            var setSourceMock = new Mock<IDbSetSource>().Object;
            var entityFinderSourceMock = new Mock<IEntityFinderSource>().Object;
            var entityGraphAttacherMock = new Mock<IEntityGraphAttacher>().Object;
            var queryProviderMock = new Mock<IAsyncQueryProvider>().Object;
            var stateManagerMock = new Mock<IStateManager>().Object;
            var exceptionDetectorMock = new Mock<IExceptionDetector>().Object;
            var updateLoggerMock = new Mock<IDiagnosticsLogger<DbLoggerCategory.Update>>().Object;
            var infrastructureLoggerMock = new Mock<IDiagnosticsLogger<DbLoggerCategory.Infrastructure>>().Object;

            var sut = new CloudStorageDbContextDependencies(
                modelMock,
                currentContextMock.Object,
                changeDetectorMock,
                setSourceMock,
                entityFinderSourceMock,
                entityGraphAttacherMock,
                queryProviderMock,
                stateManagerMock,
                exceptionDetectorMock,
                updateLoggerMock,
                infrastructureLoggerMock);

            sut.Model.ShouldBe(modelMock);
            sut.DesignTimeModel.ShouldBe(modelMock);
            sut.ChangeDetector.ShouldBe(changeDetectorMock);
            sut.SetSource.ShouldBe(setSourceMock);
            sut.EntityFinderFactory.ShouldNotBeNull();
            sut.QueryProvider.ShouldBe(queryProviderMock);
            sut.StateManager.ShouldBe(stateManagerMock);
            sut.ExceptionDetector.ShouldBe(exceptionDetectorMock);
            sut.UpdateLogger.ShouldBe(updateLoggerMock);
            sut.InfrastructureLogger.ShouldBe(infrastructureLoggerMock);
        }

        [Fact]
        public void TransactionManager_ShouldReturnInstanceOfCloudStorageTransactionManager()
        {
            var dependencies = CreateValidDependencies();

            var sut = MakeSut(
                dependencies.Item1,
                dependencies.Item2,
                dependencies.Item3,
                dependencies.Item4,
                dependencies.Item5,
                dependencies.Item6,
                dependencies.Item7,
                dependencies.Item8,
                dependencies.Item9,
                dependencies.Item10,
                dependencies.Item11
            );

            sut.TransactionManager.ShouldBeOfType<CloudStorageTransactionManager>();
        }
    }
}
