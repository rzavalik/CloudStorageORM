namespace CloudStorageORM.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BlobSettingsAttribute : ModelAttribute
{
    public string Name { get; set; }

    public BlobSettingsAttribute(string blobName)
    {
        Name = blobName ?? throw new ArgumentNullException(nameof(blobName));
        Name = blobName.Trim();
    }
}
