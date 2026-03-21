using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Tests;

public class RequiredFilesCheckerTests
{
    private static RequiredFilesChecker CreateChecker() => new();

    private static readonly string SlnxDir = OperatingSystem.IsWindows() ? @"C:\repo" : "/repo";

    #region CheckInSlnx

    [Test]
    public void CheckInSlnx_RequiredFilePresentInSlnx_ReturnsNoErrors()
    {
        // Arrange
        var requiredPath = Path.GetFullPath(Path.Combine(SlnxDir, "doc", "readme.md"));
        var slnxContent = """
            <Solution>
              <Folder Name="docs">
                <File Path="doc/readme.md" />
              </Folder>
            </Solution>
            """;

        // Act
        var errors = CreateChecker().CheckInSlnx([requiredPath], SlnxFile.Parse(slnxContent, SlnxDir)!);

        // Assert
        errors.Should().BeEmpty();
    }

    [Test]
    public void CheckInSlnx_RequiredFileMissingFromSlnx_ReturnsError()
    {
        // Arrange
        var requiredPath = Path.GetFullPath(Path.Combine(SlnxDir, "doc", "readme.md"));
        var slnxContent = """
            <Solution>
              <Folder Name="docs">
                <File Path="doc/other.md" />
              </Folder>
            </Solution>
            """;

        // Act
        var errors = CreateChecker().CheckInSlnx([requiredPath], SlnxFile.Parse(slnxContent, SlnxDir)!);

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Code.Should().Be(ValidationErrorCode.RequiredFileNotReferencedInSolution);
    }

    [Test]
    public void CheckInSlnx_ErrorMessageContainsFileElement()
    {
        // Arrange
        var requiredPath = Path.GetFullPath(Path.Combine(SlnxDir, "doc", "readme.md"));

        // Act
        var errors = CreateChecker().CheckInSlnx([requiredPath], SlnxFile.Parse("<Solution />", SlnxDir)!);

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Message.Should().Contain("<File Path=");
        errors[0].Message.Should().Contain("doc/readme.md");
    }

    [Test]
    public void CheckInSlnx_RelativeDoubleDotPath_NormalizesCorrectly()
    {
        // Arrange
        var slnxDir = OperatingSystem.IsWindows() ? @"C:\repo\sub" : "/repo/sub";
        var requiredPath = OperatingSystem.IsWindows()
            ? Path.GetFullPath(@"C:\repo\doc\readme.md")
            : Path.GetFullPath("/repo/doc/readme.md");
        var slnxContent = """
            <Solution>
              <Folder Name="docs">
                <File Path="../doc/readme.md" />
              </Folder>
            </Solution>
            """;

        // Act
        var errors = CreateChecker().CheckInSlnx([requiredPath], SlnxFile.Parse(slnxContent, slnxDir)!);

        // Assert
        errors.Should().BeEmpty();
    }

    [Test]
    public void CheckInSlnx_MultipleRequiredFiles_ReportsAllMissing()
    {
        // Arrange
        var path1 = Path.GetFullPath(Path.Combine(SlnxDir, "doc", "readme.md"));
        var path2 = Path.GetFullPath(Path.Combine(SlnxDir, "doc", "contributing.md"));

        // Act
        var errors = CreateChecker().CheckInSlnx([path1, path2], SlnxFile.Parse("<Solution />", SlnxDir)!);

        // Assert
        errors.Should().HaveCount(2);
        errors.Should().AllSatisfy(e => e.Code.Should().Be(ValidationErrorCode.RequiredFileNotReferencedInSolution));
    }

    [Test]
    public void CheckInSlnx_EmptyRequiredPaths_ReturnsNoErrors()
    {
        // Arrange
        var slnxFile = SlnxFile.Parse("<Solution />", SlnxDir)!;

        // Act
        var errors = CreateChecker().CheckInSlnx([], slnxFile);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region SlnxFile.Parse

    [Test]
    public void SlnxFileParse_ValidXml_ReturnsNonNull()
    {
        // Arrange
        var content = "<Solution />";

        // Act
        var result = SlnxFile.Parse(content, SlnxDir);

        // Assert
        result.Should().NotBeNull();
    }

    [Test]
    public void SlnxFileParse_InvalidXml_ReturnsNull()
    {
        // Arrange
        var content = "<<<not xml>>>";

        // Act
        var result = SlnxFile.Parse(content, SlnxDir);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void SlnxFileParse_FileElements_AreNormalisedToAbsolutePaths()
    {
        // Arrange
        var content = """
            <Solution>
              <File Path="doc/readme.md" />
            </Solution>
            """;

        // Act
        var result = SlnxFile.Parse(content, SlnxDir)!;

        // Assert
        result.Files.Should().HaveCount(1);
        result.Files[0].Should().Be(Path.GetFullPath(Path.Combine(SlnxDir, "doc", "readme.md")));
    }

    #endregion
}

