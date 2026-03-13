using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CloudStorageORM.Validators;

public class CloudStorageModelValidator(IStorageProvider storageProvider)
{
    private readonly IStorageProvider _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));

    public void Validate(IMutableModel model)
    {
        var cloudValidator = BlobValidatorFactory.Create(_storageProvider.CloudProvider);
        var modelValidator = new ModelValidator(cloudValidator, new BlobPathResolver(_storageProvider));

        modelValidator.Validate(model);
    }
}