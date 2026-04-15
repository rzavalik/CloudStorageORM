using Xunit.Sdk;

namespace CloudStorageORM.IntegrationTests;

internal static class IntegrationTestSkip
{
    public static void IfUnavailable(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        throw SkipException.ForSkip(reason);
    }
}