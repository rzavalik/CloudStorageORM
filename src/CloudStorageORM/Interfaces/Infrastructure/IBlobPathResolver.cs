namespace CloudStorageORM.Interfaces.Infrastructure
{
    using Microsoft.EntityFrameworkCore.Update;

    public interface IBlobPathResolver
    {
        string GetBlobName(Type type);
        string GetPath(Type type, object keyValue);
        string GetPath(IUpdateEntry entry);
    }
}