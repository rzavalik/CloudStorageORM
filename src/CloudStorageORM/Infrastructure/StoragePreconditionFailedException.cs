namespace CloudStorageORM.Infrastructure;

internal sealed class StoragePreconditionFailedException(string path, Exception? innerException = null)
    : Exception($"The object at path '{path}' was changed by another writer.", innerException)
{
    public string Path { get; } = path;
}