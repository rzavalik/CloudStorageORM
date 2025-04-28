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
        private readonly IServiceProvider _serviceProvider;
        public CloudStorageDbContextServices(DbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        public IDbContextTransactionManager TransactionManager => new CloudStorageTransactionManager();

        public ICurrentDbContext CurrentContext => new CurrentDbContext(_context);

        public IModel Model => _context.Model;

        public IModel DesignTimeModel => _context.Model;

        public IServiceProvider InternalServiceProvider => _serviceProvider;

        public DbContextOptions ContextOptions => _context.GetService<DbContextOptions>();

        public IDbContextServices Initialize(
            IServiceProvider scopedProvider, 
            DbContextOptions contextOptions, 
            DbContext context)
        {
            return new CloudStorageDbContextServices(context, scopedProvider);
        }
    }
}
