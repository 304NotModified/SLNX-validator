using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Tests;

public class RequiredFilesCheckerTests
{
    private static string CreateTempDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static RequiredFilesChecker CreateChecker() => new();

    // ── ResolveMatchedPaths (integration: 2 tests) ───────────────────────────

    [Test]
    public void ResolveMatchedPaths_SingleInclude_MatchesFiles_ReturnsNonEmpty()
    {
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            File.WriteAllText(Path.Combine(docDir, "readme.md"), "# Readme");
            File.WriteAllText(Path.Combine(docDir, "contributing.md"), "# Contributing");

            var matched = CreateChecker().ResolveMatchedPaths("doc/*.md", tempDir);

            matched.Should().HaveCount(2);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void ResolveMatchedPaths_IncludeFollowedByExclude_ExcludesFile()
    {
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            File.WriteAllText(Path.Combine(docDir, "readme.md"), "# Readme");
            File.WriteAllText(Path.Combine(docDir, "contributing.md"), "# Contributing");

            var matched = CreateChecker().ResolveMatchedPaths("doc/*.md;!doc/contributing.md", tempDir);

            matched.Should().HaveCount(1);
            matched[0].Should().EndWith("readme.md");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // ── CheckInSlnx (unit tests – no real filesystem) ────────────────────────

    private static readonly string SlnxDir = OperatingSystem.IsWindows() ? @"C:\repo" : "/repo";

    [Test]
    public void CheckInSlnx_RequiredFilePresentInSlnx_ReturnsNoErrors()
    {
        var requiredPath = Path.GetFullPath(Path.Combine(SlnxDir, "doc", "readme.md"));
        var slnxContent = """
            <Solution>
              <Folder Name="docs">
                <File Path="doc/readme.md" />
              </Folder>
            </Solution>
            """;

        var errors = CreateChecker().CheckInSlnx([requiredPath], SlnxFileRefs.Parse(slnxContent, SlnxDir));

        errors.Should().BeEmpty();
    }

    [Test]
    public void CheckInSlnx_RequiredFileMissingFromSlnx_ReturnsError()
    {
        var requiredPath = Path.GetFullPath(Path.Combine(SlnxDir, "doc", "readme.md"));
        var slnxContent = """
            <Solution>
              <Folder Name="docs">
                <File Path="doc/other.md" />
              </Folder>
            </Solution>
            """;

        var errors = CreateChecker().CheckInSlnx([requiredPath], SlnxFileRefs.Parse(slnxContent, SlnxDir));

        errors.Should().HaveCount(1);
        errors[0].Code.Should().Be(ValidationErrorCode.RequiredFileNotReferencedInSolution);
    }

    [Test]
    public void CheckInSlnx_ErrorMessageContainsFileElement()
    {
        var requiredPath = Path.GetFullPath(Path.Combine(SlnxDir, "doc", "readme.md"));

        var errors = CreateChecker().CheckInSlnx([requiredPath], SlnxFileRefs.Parse("<Solution />", SlnxDir));

        errors.Should().HaveCount(1);
        errors[0].Message.Should().Contain("<File Path=");
        errors[0].Message.Should().Contain("doc/readme.md");
    }

    [Test]
    public void CheckInSlnx_RelativeDoubleDotPath_NormalizesCorrectly()
    {
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

        var errors = CreateChecker().CheckInSlnx([requiredPath], SlnxFileRefs.Parse(slnxContent, slnxDir));

        errors.Should().BeEmpty();
    }

    [Test]
    public void CheckInSlnx_MultipleRequiredFiles_ReportsAllMissing()
    {
        var path1 = Path.GetFullPath(Path.Combine(SlnxDir, "doc", "readme.md"));
        var path2 = Path.GetFullPath(Path.Combine(SlnxDir, "doc", "contributing.md"));

        var errors = CreateChecker().CheckInSlnx([path1, path2], SlnxFileRefs.Parse("<Solution />", SlnxDir));

        errors.Should().HaveCount(2);
        errors.Should().AllSatisfy(e => e.Code.Should().Be(ValidationErrorCode.RequiredFileNotReferencedInSolution));
    }

    [Test]
    public void CheckInSlnx_EmptyRequiredPaths_ReturnsNoErrors()
    {
        var errors = CreateChecker().CheckInSlnx([], SlnxFileRefs.Parse("<Solution />", SlnxDir));

        errors.Should().BeEmpty();
    }
}

