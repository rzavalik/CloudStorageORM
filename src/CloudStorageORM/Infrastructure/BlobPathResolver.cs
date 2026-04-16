using System.Security.Cryptography;
using System.Text;
using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore.Update;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// Resolves deterministic blob names and full storage paths for EF entity types.
/// </summary>
/// <param name="storageProvider">Provider used to sanitize blob names for the active cloud backend.</param>
public class BlobPathResolver(IStorageProvider storageProvider) : IBlobPathResolver
{
    private readonly IStorageProvider _storageProvider = storageProvider
                                                         ?? throw new ArgumentNullException(
                                                             nameof(storageProvider));

    /// <inheritdoc />
    public string GetBlobName(Type type)
    {
        var name = type.Name.ToLowerInvariant();
        var hashSource = GetFullTypeName(type);
        var hash = GetDeterministicHash(hashSource);

        return $"{Sanitize(hash)}-{Sanitize(name)}";
    }

    private static string GetFullTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        var genericTypeName = type.GetGenericTypeDefinition().FullName ?? type.Name;
        var genericArgs = string.Join(",", type.GetGenericArguments().Select(GetFullTypeName));
        return $"{genericTypeName}<{genericArgs}>";
    }

    private static string GetDeterministicHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);

        return BitConverter
            .ToString(hash)
            .Replace("-", "")
            .ToLowerInvariant()[..16];
    }

    private string Sanitize(string name)
    {
        return string.IsNullOrEmpty(name)
            ? throw new ArgumentNullException(nameof(name))
            : _storageProvider.SanitizeBlobName(SanitizeLocally(name));
    }

    /// <inheritdoc />
    public string GetPath(Type type, object keyValue)
    {
        if (keyValue is null || string.IsNullOrWhiteSpace(keyValue.ToString()))
        {
            throw new InvalidOperationException(
                $"Cannot build path for entity '{type.Name}' without a valid key value.");
        }

        var blobName = GetBlobName(type);
        return $"{blobName}/{keyValue}.json";
    }

    private static string SanitizeLocally(string name)
    {
        return name
            .ToLower()
            .Replace(".", "_")
            .Replace("+", "_")
            .Replace("`", "_")
            .Replace("[", "_")
            .Replace("]", "_");
    }

    /// <inheritdoc />
    public string GetPath(IUpdateEntry entry)
    {
        var keyProperty = entry.EntityType.FindPrimaryKey()?.Properties.FirstOrDefault();
        var keyValue = entry.GetCurrentValue(keyProperty!);

        return string.IsNullOrWhiteSpace(keyValue?.ToString())
            ? throw new InvalidOperationException(
                $"Cannot persist entity '{entry.EntityType.Name}' without a valid key value.")
            : GetPath(entry.EntityType.ClrType, keyValue);
    }
}