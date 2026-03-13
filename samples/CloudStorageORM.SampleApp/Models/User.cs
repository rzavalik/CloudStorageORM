using System.ComponentModel.DataAnnotations;

namespace SampleApp.Models;

public class User
{
    [MaxLength(255)] public string Id { get; init; } = string.Empty;
    [MaxLength(255)] public string Name { get; set; } = string.Empty;
    [MaxLength(255)] public string Email { get; set; } = string.Empty;
}