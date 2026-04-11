using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CloudStorageORM.Validators;

/// <summary>
/// Validates EF models using provider-specific CloudStorageORM rules.
/// </summary>
/// <param name="storageProvider">Storage provider used to select provider-specific validation behavior.</param>
public class CloudStorageModelValidator(IStorageProvider storageProvider)
{
    private readonly IStorageProvider _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));

    /// <summary>
    /// Validates all mapped entities in the model against CloudStorageORM constraints.
    /// </summary>
    /// <param name="model">Mutable EF model to validate.</param>
    /// <example>
    /// <code>
    /// var validator = new CloudStorageModelValidator(storageProvider);
    /// validator.Validate(modelBuilder.Model);
    /// </code>
    /// </example>
    public void Validate(IMutableModel model)
    {
        var cloudValidator = BlobValidatorFactory.Create(_storageProvider.CloudProvider);
        var modelValidator = new ModelValidator(cloudValidator, new BlobPathResolver(_storageProvider));

        modelValidator.Validate(model);
    }
}