namespace CloudStorageORM.Abstractions;

/// <summary>
/// Represents a storage read result that includes the entity value, ETag, and existence status.
/// </summary>
/// <typeparam name="T">Entity payload type.</typeparam>
/// <param name="Value">Deserialized entity value when the object exists; otherwise <see langword="null" />.</param>
/// <param name="ETag">Provider ETag associated with the stored object when available.</param>
/// <param name="Exists"><see langword="true" /> when the object exists in storage; otherwise <see langword="false" />.</param>
/// <example>
/// <code>
/// var result = await storageProvider.ReadWithMetadataAsync&lt;User&gt;("users/42.json");
/// if (result.Exists)
/// {
///     Console.WriteLine(result.ETag);
/// }
/// </code>
/// </example>
public readonly record struct StorageObject<T>(T? Value, string? ETag, bool Exists);