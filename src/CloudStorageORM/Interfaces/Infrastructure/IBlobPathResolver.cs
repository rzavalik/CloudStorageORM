using Microsoft.EntityFrameworkCore.Update;

namespace CloudStorageORM.Interfaces.Infrastructure;

public interface IBlobPathResolver
{
    /// <summary>
    /// Builds the deterministic blob folder name for the specified entity type.
    /// </summary>
    /// <param name="type">Entity CLR type used to derive the blob folder.</param>
    /// <returns>The provider-safe blob folder name for the entity type.</returns>
    /// <example>
    /// <code>
    /// var folder = resolver.GetBlobName(typeof(User));
    /// // Example output: "a1b2c3d4e5f6g7h8-user"
    /// </code>
    /// </example>
    string GetBlobName(Type type);

    /// <summary>
    /// Builds the full blob path for an entity type and primary key value.
    /// </summary>
    /// <param name="type">Entity CLR type that owns the blob.</param>
    /// <param name="keyValue">Primary key value used to identify the entity blob.</param>
    /// <returns>The full blob path including folder and json file name.</returns>
    /// <example>
    /// <code>
    /// var path = resolver.GetPath(typeof(User), "42");
    /// // Example output: "a1b2c3d4e5f6g7h8-user/42.json"
    /// </code>
    /// </example>
    string GetPath(Type type, object keyValue);

    /// <summary>
    /// Builds the full blob path from an EF update entry.
    /// </summary>
    /// <param name="entry">Change-tracked EF entry that contains entity metadata and key values.</param>
    /// <returns>The full blob path for the tracked entity.</returns>
    /// <example>
    /// <code>
    /// var path = resolver.GetPath(updateEntry);
    /// </code>
    /// </example>
    string GetPath(IUpdateEntry entry);
}