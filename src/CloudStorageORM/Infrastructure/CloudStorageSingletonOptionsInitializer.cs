using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;

namespace CloudStorageORM.Infrastructure;

public class CloudStorageSingletonOptionsInitializer : ISingletonOptionsInitializer
{
    public void EnsureInitialized(IServiceProvider serviceProvider, IDbContextOptions options)
    {
    }

    public void Initialize(IServiceProvider serviceProvider, IDbContextOptions options)
    {
    }


    public void Validate(IDbContextOptions options)
    {
    }
}
