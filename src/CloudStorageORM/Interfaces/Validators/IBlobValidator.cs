namespace CloudStorageORM.Interfaces.Validators;

public interface IBlobValidator
{
    /// <summary>
    /// Validates whether a blob name complies with provider naming constraints.
    /// </summary>
    /// <param name="blobName">Blob name candidate to validate.</param>
    /// <returns><see langword="true" /> when valid; otherwise <see langword="false" />.</returns>
    /// <example>
    /// <code>
    /// if (!validator.IsBlobNameValid("users/42.json"))
    /// {
    ///     throw new InvalidOperationException("Blob name is invalid.");
    /// }
    /// </code>
    /// </example>
    bool IsBlobNameValid(string? blobName);
}