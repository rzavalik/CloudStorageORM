namespace SampleApp.Configuration
{
    using Microsoft.EntityFrameworkCore;
    public class InMemoryStorageConfiguration : IStorageConfigurationProvider
    {
        private readonly string _databaseName;

        public InMemoryStorageConfiguration(string databaseName = "TestDatabase")
        {
            _databaseName = databaseName;
        }

        public void Configure(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(_databaseName);
        }
    }
}
