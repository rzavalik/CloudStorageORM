namespace CloudStorageORM.Interfaces.Validators
{
    public interface IBlobValidator
    {
        bool IsBlobNameValid(string? blobName);
    }
}
