using System.ComponentModel.DataAnnotations;

namespace CloudStorageORM.Abstractions;

/// <summary>
/// Contract for entities that persist an object-store ETag for optimistic concurrency checks.
/// </summary>
public interface IETag
{
    /// <summary>
    /// Stores the current object-store ETag used for optimistic concurrency checks.
    /// </summary>
    /// <example>
    /// <code>
    /// public class User : IETag
    /// {
    ///     public string Id { get; set; } = string.Empty;
    ///     public string? ETag { get; set; }
    /// }
    /// </code>
    /// </example>
    [MaxLength(64)]
    string? ETag { get; set; }
}