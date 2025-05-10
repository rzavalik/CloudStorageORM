namespace CloudStorageORM.Interfaces.Infrastructure
{
    using System;
    using Microsoft.EntityFrameworkCore.Update;

    public interface IBlobPathResolver
    {
        string GetBlobName(Type type);
        string GetPath(Type entry);
        string GetPath(IUpdateEntry entry);
        string GetPath(Type entity, object id);
    }
}