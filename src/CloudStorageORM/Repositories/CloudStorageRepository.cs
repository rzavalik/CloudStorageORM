namespace CloudStorageORM.Repositories
{
    public class CloudStorageRepository<TEntity> where TEntity : class
    {
        public Task SaveAsync(string path, TEntity entity)
        {
            // TODO: Implement Save to Storage
            return Task.CompletedTask;
        }

        public Task<TEntity> ReadAsync(string path)
        {
            // TODO: Implement Read from Storage
            return Task.FromResult<TEntity>(null);
        }

        public Task<List<string>> ListAsync(string folderPath)
        {
            // TODO: Implement List from Storage
            return Task.FromResult(new List<string>());
        }

        public Task DeleteAsync(string path)
        {
            // TODO: Implement Delete from Storage
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<object>> ToListAsync()
        {
            throw new NotImplementedException();
        }
    }
}
