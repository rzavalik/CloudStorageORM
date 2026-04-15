namespace CloudStorageORM.Abstractions;

/// <summary>
/// Represents a single provider list page with continuation state.
/// </summary>
public sealed record StorageListPage(
    IReadOnlyList<string> Keys,
    string? ContinuationToken,
    bool HasMore);