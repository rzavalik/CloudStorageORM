namespace CloudStorageORM.Validators
{
    using CloudStorageORM.Enums;
    using Microsoft.EntityFrameworkCore;

    public static class CloudStorageModelValidator
    {
        public static ModelBuilder Validate(this ModelBuilder modelBuilder, CloudProvider provider)
        {
            var cloudValidator = BlobValidatorFactory.Create(provider);

            var modelValidator = new ModelValidator(cloudValidator);

            modelValidator.Validate(modelBuilder);

            return modelBuilder;
        }
    }
}