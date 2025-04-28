namespace CloudStorageORM.Tests.Repositories
{
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Repositories;
    using Moq;
    using Shouldly;
    using System;
    using System.Collections.Generic;
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
            var user = new User { Id = "1", Name = "John Doe" };
            _storageProviderMock
                .Setup(x => x.ReadAsync<User?>("user/1.json"))
                .ReturnsAsync((User?)null);

            await _repository.AddAsync("1", user);

            _storageProviderMock.Verify(x => x.SaveAsync("user/1.json", user), Times.Once);
        }

        [Fact]
        public async Task AddAsync_ShouldThrowExceptionWithMessage_WhenEntityAlreadyExists()
        {
            var existingUser = new User { Id = "1", Name = "Existing User" };
            _storageProviderMock
                .Setup(x => x.ReadAsync<User?>("user/1.json"))
                .ReturnsAsync(existingUser);

            var user = new User { Id = "1", Name = "New User" };

            var exception = await Should.ThrowAsync<Exception>(async () =>
            {
                await _repository.AddAsync("1", user);
            });

            exception.Message.ShouldContain("already exists");
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEntity_WhenEntityExists()
        {
            var user = new User { Id = "1", Name = "John Updated" };
            _storageProviderMock
                .Setup(x => x.ReadAsync<User?>("user/1.json"))
                .ReturnsAsync(new User { Id = "1", Name = "Existing User" });

            await _repository.UpdateAsync("1", user);

            _storageProviderMock.Verify(x => x.SaveAsync("user/1.json", user), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenEntityDoesNotExist()
        {
            _storageProviderMock
                .Setup(x => x.ReadAsync<User?>("user/1.json"))
                .ReturnsAsync((User?)null);

            var user = new User { Id = "1", Name = "New User" };

            var exception = await Should.ThrowAsync<Exception>(async () =>
            {
                await _repository.UpdateAsync("1", user);
            });

            exception.Message.ShouldContain("does not exist");
        }

        [Fact]
        public async Task FindAsync_ShouldReturnEntity_WhenExists()
        {
            var existingUser = new User { Id = "1", Name = "John Doe" };
            _storageProviderMock
                .Setup(x => x.ReadAsync<User?>("user/1.json"))
                .ReturnsAsync(existingUser);

            var result = await _repository.FindAsync("1");

            result.ShouldNotBeNull();
            result!.Name.ShouldBe("John Doe");
        }

        [Fact]
        public async Task FindAsync_ShouldReturnNull_WhenNotFound()
        {
            _storageProviderMock
                .Setup(x => x.ReadAsync<User?>("user/2.json"))
                .ReturnsAsync((User?)null);

            var result = await _repository.FindAsync("2");

            result.ShouldBeNull();
        }

        [Fact]
        public async Task ListAsync_ShouldReturnAllEntities()
        {
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

            var result = await _repository.ListAsync();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);
            result.ShouldContain(x => x.Id == "1" && x.Name == "User 1");
            result.ShouldContain(x => x.Id == "2" && x.Name == "User 2");
        }

        [Fact]
        public async Task RemoveAsync_ShouldCallDelete()
        {
            var id = "1";

            await _repository.RemoveAsync(id);

            _storageProviderMock.Verify(x => x.DeleteAsync("user/1.json"), Times.Once);
        }

        [Fact]
        public void EntityType_ShouldThrowNotSupportedException()
        {
            var exception = Should.Throw<NotSupportedException>(() =>
            {
                var _ = _repository.EntityType;
            });

            exception.Message.ShouldContain("Custom metadata is not supported");
        }
    }

    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
