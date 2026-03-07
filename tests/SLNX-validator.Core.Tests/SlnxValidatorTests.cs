using AwesomeAssertions;
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

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_InvalidXml_ReturnsInvalidXmlError()
    {
        var slnx = """
            this is not xml at all
            """;

        var result = await ValidatorWithFiles().ValidateAsync(slnx, RepoRoot);

        result.IsValid.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ValidationErrorCode.InvalidXml);
        result.Errors[0].Message.Should().Contain("Invalid XML");
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

        result.IsValid.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ValidationErrorCode.XsdViolation);
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

        result.IsValid.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ValidationErrorCode.XsdViolation);
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

        result.IsValid.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ValidationErrorCode.ReferencedFileNotFound);
        result.Errors[0].Message.Should().Contain("README.md");
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

        result.IsValid.Should().BeTrue();
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

        result.Errors.Should().HaveCount(2);
        foreach (var error in result.Errors)
        {
            error.Code.Should().Be(ValidationErrorCode.ReferencedFileNotFound);
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

        result.IsValid.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ValidationErrorCode.InvalidWildcardUsage);
        result.Errors[0].Message.Should().Contain("docs/*.md");
    }

    [Test]
    public async Task ValidationErrorCode_ToCode_ReturnsPrefixedCode()
    {
        ValidationErrorCode.FileNotFound.ToCode().Should().Be("SLNX001");
        ValidationErrorCode.InvalidExtension.ToCode().Should().Be("SLNX002");
        ValidationErrorCode.NotATextFile.ToCode().Should().Be("SLNX003");
        ValidationErrorCode.InvalidXml.ToCode().Should().Be("SLNX010");
        ValidationErrorCode.ReferencedFileNotFound.ToCode().Should().Be("SLNX011");
        ValidationErrorCode.InvalidWildcardUsage.ToCode().Should().Be("SLNX012");
        ValidationErrorCode.XsdViolation.ToCode().Should().Be("SLNX013");
    }
}
