using CloudStorageORM.Infrastructure;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

public class CloudStorageDatabaseCreatorTests
{
    private readonly CloudStorageDatabaseCreator _sut = new();

    [Fact]
    public void Create_DoesNotThrow() => Should.NotThrow(() => _sut.Create());

    [Fact]
    public async Task CreateAsync_CompletesSuccessfully() => await _sut.CreateAsync();

    [Fact]
    public void Delete_DoesNotThrow() => Should.NotThrow(() => _sut.Delete());

    [Fact]
    public async Task DeleteAsync_CompletesSuccessfully() => await _sut.DeleteAsync();

    [Fact]
    public void Exists_ReturnsTrue() => _sut.Exists().ShouldBeTrue();

    [Fact]
    public async Task ExistsAsync_ReturnsTrue() => (await _sut.ExistsAsync()).ShouldBeTrue();

    [Fact]
    public void HasTables_ReturnsTrue() => _sut.HasTables().ShouldBeTrue();

    [Fact]
    public async Task HasTablesAsync_ReturnsTrue() => (await _sut.HasTablesAsync()).ShouldBeTrue();

    [Fact]
    public void CanConnect_ThrowsNotImplemented()
        => Should.Throw<NotImplementedException>(() => _sut.CanConnect());

    [Fact]
    public async Task CanConnectAsync_ThrowsNotImplemented()
        => await Should.ThrowAsync<NotImplementedException>(() => _sut.CanConnectAsync());

    [Fact]
    public void EnsureCreated_ThrowsNotImplemented()
        => Should.Throw<NotImplementedException>(() => _sut.EnsureCreated());

    [Fact]
    public async Task EnsureCreatedAsync_ThrowsNotImplemented()
        => await Should.ThrowAsync<NotImplementedException>(() => _sut.EnsureCreatedAsync());

    [Fact]
    public void EnsureDeleted_ThrowsNotImplemented()
        => Should.Throw<NotImplementedException>(() => _sut.EnsureDeleted());

    [Fact]
    public async Task EnsureDeletedAsync_ThrowsNotImplemented()
        => await Should.ThrowAsync<NotImplementedException>(() => _sut.EnsureDeletedAsync());
}