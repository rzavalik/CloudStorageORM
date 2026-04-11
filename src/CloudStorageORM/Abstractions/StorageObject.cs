namespace CloudStorageORM.Abstractions;

public readonly record struct StorageObject<T>(T? Value, string? ETag, bool Exists);