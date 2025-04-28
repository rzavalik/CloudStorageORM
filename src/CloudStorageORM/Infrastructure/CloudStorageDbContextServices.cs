namespace CloudStorageORM.Infrastructure
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.DependencyInjection;
    using CloudStorageORM.DbContext;

    public class CloudStorageDbContextServices : IDbContextServices
    {
        private readonly DbContext _context;

        public CloudStorageDbContextServices(DbContext context)
        {
            _context = context;
        }

        public IDbContextTransactionManager TransactionManager => new CloudStorageTransactionManager();
        public IModel Model => _context.Model;
        public IModel DesignTimeModel => _context.Model;
        public DbContextOptions ContextOptions => _context.GetService<DbContextOptions>();
        public IServiceProvider InternalServiceProvider => _context.GetService<IServiceProvider>();
        public ICurrentDbContext CurrentContext => new CurrentDbContext(_context);

        public IDbContextServices Initialize(
            IServiceProvider scopedProvider, 
            DbContextOptions contextOptions, 
            DbContext context)
        {
            return new CloudStorageDbContextServices(context);
        }
    }
}
