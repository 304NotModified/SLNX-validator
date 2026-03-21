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

    // ── ResolveMatchedPaths ──────────────────────────────────────────────────

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

    [Test]
    public void ResolveMatchedPaths_AllExcluded_ReturnsEmpty()
    {
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            File.WriteAllText(Path.Combine(docDir, "readme.md"), "# Readme");

            var matched = CreateChecker().ResolveMatchedPaths("doc/*.md;!doc/*.md", tempDir);

            matched.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void ResolveMatchedPaths_PatternMatchesNothing_ReturnsEmpty()
    {
        var tempDir = CreateTempDir();
        try
        {
            var matched = CreateChecker().ResolveMatchedPaths("nonexistent/**/*.cs", tempDir);

            matched.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void ResolveMatchedPaths_WhitespaceAroundPatterns_IsTrimmed()
    {
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            File.WriteAllText(Path.Combine(docDir, "readme.md"), "# Readme");

            var matched = CreateChecker().ResolveMatchedPaths("  doc/*.md  ", tempDir);

            matched.Should().HaveCount(1);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void ResolveMatchedPaths_EmptyPatternEntries_AreDiscarded()
    {
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            File.WriteAllText(Path.Combine(docDir, "readme.md"), "# Readme");

            var matched = CreateChecker().ResolveMatchedPaths(";;doc/*.md;;", tempDir);

            matched.Should().HaveCount(1);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // ── CheckInSlnx ─────────────────────────────────────────────────────────

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

        var errors = CreateChecker().CheckInSlnx([requiredPath], slnxContent, SlnxDir);

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

        var errors = CreateChecker().CheckInSlnx([requiredPath], slnxContent, SlnxDir);

        errors.Should().HaveCount(1);
        errors[0].Code.Should().Be(ValidationErrorCode.RequiredFileNotReferencedInSolution);
    }

    [Test]
    public void CheckInSlnx_ErrorMessageContainsFileElement()
    {
        var requiredPath = Path.GetFullPath(Path.Combine(SlnxDir, "doc", "readme.md"));
        var slnxContent = "<Solution />";

        var errors = CreateChecker().CheckInSlnx([requiredPath], slnxContent, SlnxDir);

        errors.Should().HaveCount(1);
        errors[0].Message.Should().Contain("<File Path=");
        errors[0].Message.Should().Contain("doc/readme.md");
    }

    [Test]
    public void CheckInSlnx_RelativeDoubleDotPath_NormalizesCorrectly()
    {
        // slnx is in /repo/sub, the File path uses ".." to reach /repo/doc/readme.md
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

        var errors = CreateChecker().CheckInSlnx([requiredPath], slnxContent, slnxDir);

        errors.Should().BeEmpty();
    }

    [Test]
    public void CheckInSlnx_MultipleRequiredFiles_ReportsAllMissing()
    {
        var path1 = Path.GetFullPath(Path.Combine(SlnxDir, "doc", "readme.md"));
        var path2 = Path.GetFullPath(Path.Combine(SlnxDir, "doc", "contributing.md"));
        var slnxContent = "<Solution />";

        var errors = CreateChecker().CheckInSlnx([path1, path2], slnxContent, SlnxDir);

        errors.Should().HaveCount(2);
        errors.Should().AllSatisfy(e => e.Code.Should().Be(ValidationErrorCode.RequiredFileNotReferencedInSolution));
    }

    [Test]
    public void CheckInSlnx_EmptyRequiredPaths_ReturnsNoErrors()
    {
        var slnxContent = "<Solution />";

        var errors = CreateChecker().CheckInSlnx([], slnxContent, SlnxDir);

        errors.Should().BeEmpty();
    }
}
