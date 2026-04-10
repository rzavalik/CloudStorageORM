using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CloudStorageORM.Abstractions;

namespace SampleApp.Models;

public class User : IETag
{
    [MaxLength(255)] public string Id { get; init; } = string.Empty;
    [MaxLength(255)] public string Name { get; set; } = string.Empty;
    [MaxLength(255)] public string Email { get; set; } = string.Empty;

    [JsonIgnore]
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string? ETag { get; set; }
}