namespace CloudStorageORM.Interfaces.Repositories;

public interface ICloudStorageRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Adds a new entity to storage for the provided identifier.
    /// </summary>
    /// <param name="id">Entity identifier used to build the storage path.</param>
    /// <param name="entity">Entity instance to persist.</param>
    /// <example>
    /// <code>
    /// await repository.AddAsync("42", user);
    /// </code>
    /// </example>
    Task AddAsync(string id, TEntity entity);

    /// <summary>
    /// Updates an existing entity in storage for the provided identifier.
    /// </summary>
    /// <param name="id">Entity identifier used to locate the storage object.</param>
    /// <param name="entity">Updated entity instance to persist.</param>
    /// <example>
    /// <code>
    /// await repository.UpdateAsync("42", user);
    /// </code>
    /// </example>
    Task UpdateAsync(string id, TEntity entity);

    /// <summary>
    /// Finds an entity by identifier.
    /// </summary>
    /// <param name="id">Entity identifier used to locate the storage object.</param>
    /// <returns>The matching entity when found.</returns>
    /// <example>
    /// <code>
    /// var user = await repository.FindAsync("42");
    /// </code>
    /// </example>
    Task<TEntity> FindAsync(string id);

    /// <summary>
    /// Lists all entities available for this repository type.
    /// </summary>
    /// <returns>A list containing all stored entities.</returns>
    /// <example>
    /// <code>
    /// var users = await repository.ListAsync();
    /// </code>
    /// </example>
    Task<List<TEntity>> ListAsync();

    /// <summary>
    /// Removes the entity identified by <paramref name="id" /> from storage.
    /// </summary>
    /// <param name="id">Entity identifier used to locate the storage object.</param>
    /// <example>
    /// <code>
    /// await repository.RemoveAsync("42");
    /// </code>
    /// </example>
    Task RemoveAsync(string id);
}