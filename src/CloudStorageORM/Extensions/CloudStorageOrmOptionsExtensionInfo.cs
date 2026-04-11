using CloudStorageORM.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CloudStorageORM.Extensions;

/// <summary>
/// EF Core extension metadata used to describe CloudStorageORM options in logging and service-provider caching.
/// </summary>
public class CloudStorageOrmOptionsExtensionInfo(CloudStorageOrmOptionsExtension extension)
    : DbContextOptionsExtensionInfo(extension)
{
    /// <summary>
    /// Gets a short provider-specific log fragment used by EF diagnostics.
    /// </summary>
    public override string LogFragment => $"CloudStorageORM ({Extension.Options.Provider}): {GetProviderLogValue()}";

    /// <summary>
    /// Gets a value indicating this extension represents a database provider.
    /// </summary>
    public override bool IsDatabaseProvider => true;

    /// <summary>
    /// Gets the typed CloudStorageORM options extension.
    /// </summary>
    public new CloudStorageOrmOptionsExtension Extension => (CloudStorageOrmOptionsExtension)base.Extension;

    /// <summary>
    /// Computes a stable hash code based on provider-specific option values.
    /// </summary>
    /// <returns>A hash code used by EF Core service-provider caching.</returns>
    /// <example>
    /// <code>
    /// var hash = extensionInfo.GetServiceProviderHashCode();
    /// </code>
    /// </example>
    public override int GetServiceProviderHashCode()
    {
        var options = Extension.Options;
        var raw = options.Provider switch
        {
            Enums.CloudProvider.Azure =>
                $"azure|{options.ContainerName}|{options.Azure.ConnectionString}",
            Enums.CloudProvider.Aws =>
                $"aws|{options.ContainerName}|{options.Aws.Region}|{options.Aws.ServiceUrl}|{options.Aws.AccessKeyId}|{options.Aws.SecretAccessKey}|{options.Aws.ForcePathStyle}",
            _ =>
                $"{options.Provider}|{options.ContainerName}"
        };

        return StringComparer.Ordinal.GetHashCode(raw);
    }

    /// <summary>
    /// Adds provider-specific option values to EF Core debug information.
    /// </summary>
    /// <param name="debugInfo">Dictionary populated with diagnostics values.</param>
    /// <example>
    /// <code>
    /// var debugInfo = new Dictionary&lt;string, string&gt;();
    /// extensionInfo.PopulateDebugInfo(debugInfo);
    /// </code>
    /// </example>
    public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
    {
        var options = Extension.Options;
        debugInfo["CloudStorageORM:Provider"] = options.Provider.ToString();
        debugInfo["CloudStorageORM:ContainerName"] = options.ContainerName;

        switch (options.Provider)
        {
            case Enums.CloudProvider.Azure:
                debugInfo["CloudStorageORM:Azure:ConnectionString"] = options.Azure.ConnectionString;
                break;
            case Enums.CloudProvider.Aws:
                debugInfo["CloudStorageORM:Aws:Region"] = options.Aws.Region;
                debugInfo["CloudStorageORM:Aws:ServiceUrl"] = options.Aws.ServiceUrl;
                debugInfo["CloudStorageORM:Aws:AccessKeyId"] = options.Aws.AccessKeyId;
                debugInfo["CloudStorageORM:Aws:ForcePathStyle"] = options.Aws.ForcePathStyle.ToString();
                break;
        }
    }

    /// <summary>
    /// Determines whether EF Core can reuse the same internal service provider for another extension instance.
    /// </summary>
    /// <param name="other">The other extension info instance to compare.</param>
    /// <returns><see langword="false" /> because CloudStorageORM always forces a distinct service provider.</returns>
    /// <example>
    /// <code>
    /// var canReuse = extensionInfo.ShouldUseSameServiceProvider(otherInfo);
    /// </code>
    /// </example>
    public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => false;

    private string GetProviderLogValue()
    {
        var options = Extension.Options;
        return options.Provider switch
        {
            Enums.CloudProvider.Azure => options.Azure.ConnectionString,
            Enums.CloudProvider.Aws => $"region={options.Aws.Region};serviceUrl={options.Aws.ServiceUrl};container={options.ContainerName}",
            _ => options.ContainerName
        };
    }
}