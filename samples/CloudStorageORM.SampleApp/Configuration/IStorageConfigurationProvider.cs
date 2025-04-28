namespace SampleApp.Configuration
{
    using Microsoft.EntityFrameworkCore;

    public interface IStorageConfigurationProvider
    {
        void Configure(DbContextOptionsBuilder optionsBuilder);
    }
}