namespace CloudStorageORM.Validators
{
    using CloudStorageORM.Enums;
    using Microsoft.EntityFrameworkCore.Metadata;

    public class CloudStorageModelValidator
    {
        private readonly CloudProvider _cloudProvider;

        public CloudStorageModelValidator(CloudProvider cloudProvider)
        {
            _cloudProvider = cloudProvider;
        }

        public void Validate(IMutableModel model)
        {
            var cloudValidator = BlobValidatorFactory.Create(_cloudProvider);
            var modelValidator = new ModelValidator(cloudValidator);

            modelValidator.Validate(model);
        }
    }
}