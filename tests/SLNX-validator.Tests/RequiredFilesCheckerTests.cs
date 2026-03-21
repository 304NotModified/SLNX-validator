using AwesomeAssertions;

namespace JulianVerdurmen.SlnxValidator.Tests;

public class RequiredFilesCheckerTests
{
    private static string CreateTempDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    [Test]
    public async Task CheckAsync_SingleIncludePattern_MatchesFiles_ReturnsZero()
    {
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            await File.WriteAllTextAsync(Path.Combine(docDir, "readme.md"), "# Readme");
            await File.WriteAllTextAsync(Path.Combine(docDir, "contributing.md"), "# Contributing");

            var exitCode = await RequiredFilesChecker.CheckAsync("doc/*.md", tempDir);

            exitCode.Should().Be(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CheckAsync_IncludeFollowedByExclude_ExcludesFiles_ReturnsZero()
    {
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            await File.WriteAllTextAsync(Path.Combine(docDir, "readme.md"), "# Readme");
            await File.WriteAllTextAsync(Path.Combine(docDir, "contributing.md"), "# Contributing");

            // Include all .md in doc/, then exclude contributing.md — should still match (readme.md)
            var exitCode = await RequiredFilesChecker.CheckAsync("doc/*.md;!doc/contributing.md", tempDir);

            exitCode.Should().Be(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CheckAsync_IncludeFollowedByExcludeAll_NoMatches_ReturnsNonZero()
    {
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            await File.WriteAllTextAsync(Path.Combine(docDir, "readme.md"), "# Readme");

            // Include then exclude everything → no matches
            var exitCode = await RequiredFilesChecker.CheckAsync("doc/*.md;!doc/*.md", tempDir);

            exitCode.Should().NotBe(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CheckAsync_ExcludeFollowedByReInclude_RestoresFile_ReturnsZero()
    {
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            await File.WriteAllTextAsync(Path.Combine(docDir, "readme.md"), "# Readme");
            await File.WriteAllTextAsync(Path.Combine(docDir, "contributing.md"), "# Contributing");

            // Exclude all md, then re-include readme.md → readme.md should match
            var exitCode = await RequiredFilesChecker.CheckAsync("doc/*.md;!doc/*.md;doc/readme.md", tempDir);

            exitCode.Should().Be(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CheckAsync_PatternWithNoMatches_ReturnsNonZero()
    {
        var tempDir = CreateTempDir();
        try
        {
            var exitCode = await RequiredFilesChecker.CheckAsync("nonexistent/**/*.cs", tempDir);

            exitCode.Should().NotBe(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CheckAsync_WhitespaceAroundPatterns_IsTrimmed_ReturnsZero()
    {
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            await File.WriteAllTextAsync(Path.Combine(docDir, "readme.md"), "# Readme");

            var exitCode = await RequiredFilesChecker.CheckAsync("  doc/*.md  ", tempDir);

            exitCode.Should().Be(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CheckAsync_EmptyPatternEntries_AreDiscarded_ReturnsZero()
    {
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            await File.WriteAllTextAsync(Path.Combine(docDir, "readme.md"), "# Readme");

            var exitCode = await RequiredFilesChecker.CheckAsync(";;doc/*.md;;", tempDir);

            exitCode.Should().Be(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CheckInSlnxAsync_RequiredFilePresentInSlnx_ReturnsZero()
    {
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            var readmePath = Path.Combine(docDir, "readme.md");
            await File.WriteAllTextAsync(readmePath, "# Readme");

            var slnxPath = Path.Combine(tempDir, "solution.slnx");
            await File.WriteAllTextAsync(slnxPath, """
                <Solution>
                  <Folder Name="docs">
                    <File Path="doc/readme.md" />
                  </Folder>
                </Solution>
                """);

            var requiredAbsolutePaths = new[] { Path.GetFullPath(readmePath) };
            var exitCode = await RequiredFilesChecker.CheckInSlnxAsync(requiredAbsolutePaths, [slnxPath]);

            exitCode.Should().Be(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CheckInSlnxAsync_RequiredFileMissingFromSlnx_ReturnsTwo()
    {
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            var readmePath = Path.Combine(docDir, "readme.md");
            await File.WriteAllTextAsync(readmePath, "# Readme");

            var slnxPath = Path.Combine(tempDir, "solution.slnx");
            await File.WriteAllTextAsync(slnxPath, """
                <Solution>
                  <Folder Name="docs">
                    <File Path="doc/other.md" />
                  </Folder>
                </Solution>
                """);

            var requiredAbsolutePaths = new[] { Path.GetFullPath(readmePath) };
            var exitCode = await RequiredFilesChecker.CheckInSlnxAsync(requiredAbsolutePaths, [slnxPath]);

            exitCode.Should().Be(2);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CheckInSlnxAsync_RelativePathsNormalisedCorrectly_ReturnsZero()
    {
        var tempDir = CreateTempDir();
        try
        {
            var subDir = Path.Combine(tempDir, "sub");
            Directory.CreateDirectory(subDir);
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            var readmePath = Path.Combine(docDir, "readme.md");
            await File.WriteAllTextAsync(readmePath, "# Readme");

            // .slnx is in a subdirectory — path uses ".." to reach doc/readme.md
            var slnxPath = Path.Combine(subDir, "solution.slnx");
            await File.WriteAllTextAsync(slnxPath, """
                <Solution>
                  <Folder Name="docs">
                    <File Path="../doc/readme.md" />
                  </Folder>
                </Solution>
                """);

            var requiredAbsolutePaths = new[] { Path.GetFullPath(readmePath) };
            var exitCode = await RequiredFilesChecker.CheckInSlnxAsync(requiredAbsolutePaths, [slnxPath]);

            exitCode.Should().Be(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CheckInSlnxAsync_NoSlnxFiles_ReturnsTwoForEachRequired()
    {
        var tempDir = CreateTempDir();
        try
        {
            var readmePath = Path.GetFullPath(Path.Combine(tempDir, "readme.md"));
            var exitCode = await RequiredFilesChecker.CheckInSlnxAsync([readmePath], []);

            exitCode.Should().Be(2);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
