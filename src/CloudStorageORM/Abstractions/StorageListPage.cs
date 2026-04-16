namespace CloudStorageORM.Abstractions;

/// <summary>
/// Represents a single provider list page with continuation state.
/// </summary>
/// <param name="Keys">The object keys returned in this page.</param>
/// <param name="ContinuationToken">The token that can be used to request the next page, when available.</param>
/// <param name="HasMore">Indicates whether more pages are available.</param>
public sealed record StorageListPage(
    IReadOnlyList<string> Keys,
    string? ContinuationToken,
    bool HasMore);