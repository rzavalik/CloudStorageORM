namespace CloudStorageORM.Tests.Azure.Validators
{
    using CloudStorageORM.Providers.Azure.Validators;
    using Shouldly;

    public class AzureBlobValidatorTests
    {
        private AzureBlobValidator MakeSut()
        {
            return new AzureBlobValidator();
        }

        [Theory]
        [InlineData("validblobname")]
        [InlineData("folder/validblob")]
        [InlineData("folder/subfolder/blobname")]
        public void IsBlobNameValid_ValidNames_ShouldReturnTrue(string blobName)
        {
            var sut = MakeSut();

            var result = sut.IsBlobNameValid(blobName);

            result.ShouldBeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        [InlineData("InvalidBlobName")] // Uppercase
        [InlineData("folder//blob")] // Double forward slash
        [InlineData("folder\\\\blob")] // Backslashes
        [InlineData("\\folder\\blob")] // Starts with backslash
        [InlineData("folder\\blob")] // Contains backslash
        [InlineData("/folder/blob")] // Starts with slash
        [InlineData("folder/blob/")] // Ends with slash
        [InlineData("folder..blob")] // Double dot
        [InlineData("folder/blob?name")] // Invalid char '?'
        [InlineData("folder/blob*name")] // Invalid char '*'
        [InlineData("folder/blob:name")] // Invalid char ':'
        [InlineData("folder/blob|name")] // Invalid char '|'
        [InlineData("folder/blob\"name")] // Invalid char '"'
        [InlineData("folder/blob<name")] // Invalid char '<'
        [InlineData("folder/blob>name")] // Invalid char '>' 
        public void IsBlobNameValid_InvalidNames_ShouldReturnFalse(string blobName)
        {
            var sut = MakeSut();

            var result = sut.IsBlobNameValid(blobName);

            result.ShouldBeFalse();
        }

        [Fact]
        public void IsBlobNameValid_NameExactlyAtLimit_ShouldReturnTrue()
        {
            var sut = MakeSut();
            var blobName = new string('a', 1024);

            var result = sut.IsBlobNameValid(blobName);

            result.ShouldBeTrue();
        }

        [Fact]
        public void IsBlobNameValid_NameAboveLimit_ShouldReturnFalse()
        {
            var sut = MakeSut();
            var blobName = new string('a', 1025);

            var result = sut.IsBlobNameValid(blobName);

            result.ShouldBeFalse();
        }
    }
}