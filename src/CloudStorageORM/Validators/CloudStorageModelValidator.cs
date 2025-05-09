namespace CloudStorageORM.Validators
{
    using CloudStorageORM.Infrastructure;
    using CloudStorageORM.Interfaces.StorageProviders;
    using Microsoft.EntityFrameworkCore.Metadata;

    public class CloudStorageModelValidator
    {
        private readonly IStorageProvider _storageProvider;

        public CloudStorageModelValidator(
            IStorageProvider storageProvider)
        {
            _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        }

        public void Validate(IMutableModel model)
        {
            var cloudValidator = BlobValidatorFactory.Create(_storageProvider.CloudProvider);
            var modelValidator = new ModelValidator(cloudValidator, new BlobPathResolver(_storageProvider));

            modelValidator.Validate(model);
        }
    }
}