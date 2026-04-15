using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// Singleton options initializer for CloudStorageORM EF services.
/// </summary>
public class CloudStorageSingletonOptionsInitializer : ISingletonOptionsInitializer
{
    /// <summary>
    /// Ensures singleton options are initialized for the current service provider scope.
    /// </summary>
    /// <param name="serviceProvider">Service provider used to resolve singleton option services.</param>
    /// <param name="options">DbContext options being initialized.</param>
    /// <remarks>
    /// CloudStorageORM currently requires no additional singleton initialization in this phase.
    /// </remarks>
    public void EnsureInitialized(IServiceProvider serviceProvider, IDbContextOptions options)
    {
    }

    /// <summary>
    /// Initializes singleton options for CloudStorageORM.
    /// </summary>
    /// <param name="serviceProvider">Service provider used to resolve singleton option services.</param>
    /// <param name="options">DbContext options being initialized.</param>
    /// <remarks>
    /// CloudStorageORM currently performs no additional work in this method.
    /// </remarks>
    public void Initialize(IServiceProvider serviceProvider, IDbContextOptions options)
    {
    }


    /// <summary>
    /// Validates singleton options for CloudStorageORM.
    /// </summary>
    /// <param name="options">DbContext options to validate.</param>
    /// <remarks>
    /// CloudStorageORM currently has no extra singleton option validation in this method.
    /// </remarks>
    public void Validate(IDbContextOptions options)
    {
    }
}