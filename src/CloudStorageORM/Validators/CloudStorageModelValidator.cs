namespace CloudStorageORM.Validators
{
    using CloudStorageORM.Enums;
    using Microsoft.EntityFrameworkCore.Metadata;

    public static class CloudStorageModelValidator 
    {
        public static void Validate(this IModel model, CloudProvider provider)
        {
            var cloudValidator = BlobValidatorFactory.Create(provider);
            var modelValidator = new ModelValidator(cloudValidator);

            modelValidator.Validate(model);
        }
    }
}