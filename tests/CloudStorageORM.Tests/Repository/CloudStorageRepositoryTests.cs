namespace CloudStorageORM.Tests.Repositories
{
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Repositories;
    using Moq;
    using Shouldly;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    public class CloudStorageRepositoryTests
    {
        private readonly Mock<IStorageProvider> _storageProviderMock;
        private readonly CloudStorageRepository<User> _repository;

        public CloudStorageRepositoryTests()
        {
            _storageProviderMock = new Mock<IStorageProvider>();
            _repository = new CloudStorageRepository<User>(_storageProviderMock.Object);
        }

        [Fact]
        public async Task AddAsync_ShouldSaveEntity_WhenEntityDoesNotExist()
        {
            // Arrange
            var user = new User { Id = "1", Name = "John Doe" };
            _storageProviderMock
                .Setup(x => x.ReadAsync<User?>("user/1.json"))
                .ReturnsAsync((User?)null); // Entity does not exist

            // Act
            await _repository.AddAsync("1", user);

            // Assert
            _storageProviderMock.Verify(x => x.SaveAsync("user/1.json", user), Times.Once);
        }

        [Fact]
        public async Task AddAsync_ShouldThrowException_WhenEntityAlreadyExists()
        {
            // Arrange
            var existingUser = new User { Id = "1", Name = "Existing User" };
            _storageProviderMock
                .Setup(x => x.ReadAsync<User?>("user/1.json"))
                .ReturnsAsync(existingUser); // Entity already exists

            var user = new User { Id = "1", Name = "New User" };

            // Act & Assert
            await Should.ThrowAsync<Exception>(async () =>
            {
                await _repository.AddAsync("1", user);
            });
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEntity_WhenEntityExists()
        {
            // Arrange
            var user = new User { Id = "1", Name = "John Updated" };
            _storageProviderMock
                .Setup(x => x.ReadAsync<User?>("user/1.json"))
                .ReturnsAsync(new User { Id = "1", Name = "Existing User" });

            // Act
            await _repository.UpdateAsync("1", user);

            // Assert
            _storageProviderMock.Verify(x => x.SaveAsync("user/1.json", user), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenEntityDoesNotExist()
        {
            // Arrange
            _storageProviderMock
                .Setup(x => x.ReadAsync<User?>("user/1.json"))
                .ReturnsAsync((User?)null);

            var user = new User { Id = "1", Name = "New User" };

            // Act & Assert
            await Should.ThrowAsync<Exception>(async () =>
            {
                await _repository.UpdateAsync("1", user);
            });
        }

        [Fact]
        public async Task FindAsync_ShouldReturnEntity_WhenExists()
        {
            // Arrange
            var existingUser = new User { Id = "1", Name = "John Doe" };
            _storageProviderMock
                .Setup(x => x.ReadAsync<User?>("user/1.json"))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _repository.FindAsync("1");

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe("John Doe");
        }

        [Fact]
        public async Task FindAsync_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            _storageProviderMock
                .Setup(x => x.ReadAsync<User?>("user/2.json"))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _repository.FindAsync("2");

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task ListAsync_ShouldReturnAllEntities()
        {
            // Arrange
            var paths = new List<string> { "user/1.json", "user/2.json" };
            _storageProviderMock
                .Setup(x => x.ListAsync("user"))
                .ReturnsAsync(paths);

            _storageProviderMock
                .Setup(x => x.ReadAsync<User?>("user/1.json"))
                .ReturnsAsync(new User { Id = "1", Name = "User 1" });

            _storageProviderMock
                .Setup(x => x.ReadAsync<User?>("user/2.json"))
                .ReturnsAsync(new User { Id = "2", Name = "User 2" });

            // Act
            var result = await _repository.ListAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);
        }

        [Fact]
        public async Task RemoveAsync_ShouldCallDelete()
        {
            // Arrange
            var id = "1";

            // Act
            await _repository.RemoveAsync(id);

            // Assert
            _storageProviderMock.Verify(x => x.DeleteAsync("user/1.json"), Times.Once);
        }

    }

    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}