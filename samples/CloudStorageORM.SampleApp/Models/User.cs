using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CloudStorageORM.Abstractions;

namespace SampleApp.Models;

/// <summary>
/// Sample entity used to demonstrate CloudStorageORM CRUD and ETag concurrency behavior.
/// </summary>
public class User : IETag
{
    /// <summary>
    /// Primary identifier persisted as part of the storage object key.
    /// </summary>
    [MaxLength(255)] public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Display name for the sample user.
    /// </summary>
    [MaxLength(255)] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address for the sample user.
    /// </summary>
    [MaxLength(255)] public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Object-store ETag used for optimistic concurrency checks.
    /// </summary>
    [JsonIgnore]
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string? ETag { get; set; }
}