namespace CloudStorageORM.Abstractions;

/// <summary>
/// Declares blob settings metadata for an entity type.
/// </summary>
/// <example>
/// <code>
/// [BlobSettings("users")]
/// public class User
/// {
///     public string Id { get; set; } = string.Empty;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BlobSettingsAttribute : ModelAttribute
{
    /// <summary>
    /// Gets or sets the logical blob folder name for the annotated entity type.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Initializes a new attribute with a blob folder name.
    /// </summary>
    /// <param name="blobName">Blob folder name to associate with the entity type.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="blobName" /> is <see langword="null" />.</exception>
    /// <example>
    /// <code>
    /// [BlobSettings("orders")]
    /// public class Order { }
    /// </code>
    /// </example>
    public BlobSettingsAttribute(string blobName)
    {
        Name = blobName ?? throw new ArgumentNullException(nameof(blobName));
        Name = blobName.Trim();
    }
}