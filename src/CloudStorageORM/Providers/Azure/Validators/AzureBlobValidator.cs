using CloudStorageORM.Interfaces.Validators;

namespace CloudStorageORM.Providers.Azure.Validators;

public class AzureBlobValidator : IBlobValidator
{
    public bool IsBlobNameValid(string? blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName) || blobName.Length > 1024)
        {
            return false;
        }

        if (blobName.Any(char.IsUpper))
        {
            return false;
        }

        if (blobName.Contains(".."))
        {
            return false;
        }

        if (blobName.StartsWith("/") || blobName.EndsWith("/") ||
            blobName.StartsWith("\\") || blobName.EndsWith("\\"))
        {
            return false;
        }

        if (blobName.Contains("\\"))
        {
            return false;
        }

        if (blobName.Contains("//"))
        {
            return false;
        }

        if (blobName.IndexOfAny(['?', '%', '*', ':', '|', '"', '<', '>']) >= 0)
        {
            return false;
        }

        return true;
    }
}