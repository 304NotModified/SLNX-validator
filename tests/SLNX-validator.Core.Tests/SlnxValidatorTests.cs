using JulianVerdurmen.SlnxValidator.Core.Validation;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Tests;

public class SlnxValidatorTests
{
    private static Validation.SlnxValidator ValidatorWithFiles(params string[] existingPaths)
        => new(new MockFileSystem(existingPaths), new XsdValidator());

    private static readonly string RepoRoot = OperatingSystem.IsWindows() ? @"C:\repo" : "/repo";

    [Test]
    public async Task ValidateAsync_EmptySolution_IsValid()
    {
        var slnx = """
            <Solution>
            </Solution>
            """;

        var result = await ValidatorWithFiles().ValidateAsync(slnx, RepoRoot);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task ValidateAsync_InvalidXml_ReturnsInvalidXmlError()
    {
        var slnx = """
            this is not xml at all
            """;

        var result = await ValidatorWithFiles().ValidateAsync(slnx, RepoRoot);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ValidationErrorCode.InvalidXml);
        await Assert.That(result.Errors[0].Message).Contains("Invalid XML");
    }

    [Test]
    public async Task ValidateAsync_XsdViolation_ReturnsXsdViolationError()
    {
        var slnx = """
            <Solution>
              <UnknownElement />
            </Solution>
            """;

        var result = await ValidatorWithFiles().ValidateAsync(slnx, RepoRoot);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ValidationErrorCode.XsdViolation);
    }

    [Test]
    public async Task ValidateAsync_ProjectWithoutPathAttribute_ReturnsXsdViolationError()
    {
        var slnx = """
            <Solution>
              <Project />
            </Solution>
            """;

        // Path is use="required" in the XSD, so this is caught as an XSD violation
        var result = await ValidatorWithFiles().ValidateAsync(slnx, RepoRoot);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ValidationErrorCode.XsdViolation);
    }

    [Test]
    public async Task ValidateAsync_MissingFileInFolder_ReturnsFileNotFoundError()
    {
        var slnx = """
            <Solution>
              <Folder Name="docs">
                <File Path="README.md" />
              </Folder>
            </Solution>
            """;

        var result = await ValidatorWithFiles().ValidateAsync(slnx, RepoRoot);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ValidationErrorCode.ReferencedFileNotFound);
        await Assert.That(result.Errors[0].Message).Contains("README.md");
    }

    [Test]
    public async Task ValidateAsync_ExistingFileInFolder_IsValid()
    {
        var slnx = """
            <Solution>
              <Folder Name="docs">
                <File Path="README.md" />
              </Folder>
            </Solution>
            """;

        var result = await ValidatorWithFiles(Path.Combine(RepoRoot, "README.md"))
            .ValidateAsync(slnx, RepoRoot);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task ValidateAsync_MultipleErrors_AllReported()
    {
        var slnx = """
            <Solution>
              <Folder Name="docs">
                <File Path="missing/One.md" />
                <File Path="missing/Two.md" />
              </Folder>
            </Solution>
            """;

        var result = await ValidatorWithFiles().ValidateAsync(slnx, RepoRoot);

        await Assert.That(result.Errors.Count).IsEqualTo(2);
        foreach (var error in result.Errors)
        {
            await Assert.That(error.Code).IsEqualTo(ValidationErrorCode.ReferencedFileNotFound);
        }
    }

    [Test]
    public async Task ValidateAsync_WildcardInFilePath_ReturnsInvalidWildcardUsageError()
    {
        var slnx = """
            <Solution>
              <Folder Name="docs">
                <File Path="docs/*.md" />
              </Folder>
            </Solution>
            """;

        var result = await ValidatorWithFiles().ValidateAsync(slnx, RepoRoot);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ValidationErrorCode.InvalidWildcardUsage);
        await Assert.That(result.Errors[0].Message).Contains("docs/*.md");
    }

    [Test]
    public async Task ValidationErrorCode_ToCode_ReturnsPrefixedCode()
    {
        await Assert.That(ValidationErrorCode.FileNotFound.ToCode()).IsEqualTo("SLNX0001");
        await Assert.That(ValidationErrorCode.InvalidExtension.ToCode()).IsEqualTo("SLNX0002");
        await Assert.That(ValidationErrorCode.NotATextFile.ToCode()).IsEqualTo("SLNX0003");
        await Assert.That(ValidationErrorCode.InvalidXml.ToCode()).IsEqualTo("SLNX0010");
        await Assert.That(ValidationErrorCode.ReferencedFileNotFound.ToCode()).IsEqualTo("SLNX0011");
        await Assert.That(ValidationErrorCode.InvalidWildcardUsage.ToCode()).IsEqualTo("SLNX0012");
        await Assert.That(ValidationErrorCode.XsdViolation.ToCode()).IsEqualTo("SLNX0013");
    }
}
