using Microsoft.EntityFrameworkCore.Update;

namespace CloudStorageORM.Interfaces.Infrastructure;

public interface IBlobPathResolver
{
    string GetBlobName(Type type);
    string GetPath(Type type, object keyValue);
    string GetPath(IUpdateEntry entry);
}