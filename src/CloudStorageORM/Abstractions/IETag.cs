using System.ComponentModel.DataAnnotations;

namespace CloudStorageORM.Abstractions;

public interface IETag
{
    [MaxLength(64)]
    string? ETag { get; set; }
}