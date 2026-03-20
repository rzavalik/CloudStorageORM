using CloudStorageORM.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CloudStorageORM.Extensions;

public class CloudStorageOrmOptionsExtensionInfo(CloudStorageOrmOptionsExtension extension)
    : DbContextOptionsExtensionInfo(extension)
{
    public override string LogFragment => $"CloudStorageORM ({Extension.Options.Provider}): {GetProviderLogValue()}";

    public override bool IsDatabaseProvider => true;

    public new CloudStorageOrmOptionsExtension Extension => (CloudStorageOrmOptionsExtension)base.Extension;

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