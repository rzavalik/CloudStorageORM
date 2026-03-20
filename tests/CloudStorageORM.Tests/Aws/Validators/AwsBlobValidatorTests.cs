using CloudStorageORM.Providers.Aws.Validators;
using Shouldly;

namespace CloudStorageORM.Tests.Aws.Validators;

public class AwsBlobValidatorTests
{
    private static AwsBlobValidator MakeSut()
    {
        return new AwsBlobValidator();
    }

    [Theory]
    [InlineData("validblobname")]
    [InlineData("folder/valid-blob")]
    [InlineData("folder/subfolder/blob_001")]
    [InlineData("a.b")]
    public void IsBlobNameValid_ValidNames_ShouldReturnTrue(string blobName)
    {
        var sut = MakeSut();

        var result = sut.IsBlobNameValid(blobName);

        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("InvalidBlobName")]
    [InlineData("folder..blob")]
    [InlineData("/folder/blob")]
    [InlineData("folder/blob/")]
    [InlineData("\\folder/blob")]
    [InlineData("folder/blob\\")]
    [InlineData("folder\\blob")]
    [InlineData("folder//blob")]
    [InlineData("folder/blob?name")]
    [InlineData("folder/blob%name")]
    [InlineData("folder/blob*name")]
    [InlineData("folder/blob:name")]
    [InlineData("folder/blob|name")]
    [InlineData("folder/blob\"name")]
    [InlineData("folder/blob<name")]
    [InlineData("folder/blob>name")]
    public void IsBlobNameValid_InvalidNames_ShouldReturnFalse(string? blobName)
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