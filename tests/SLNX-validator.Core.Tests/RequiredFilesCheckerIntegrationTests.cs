using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.Validation;

namespace JulianVerdurmen.SlnxValidator.Core.Tests;

public class RequiredFilesCheckerIntegrationTests
{
    private static string CreateTempDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static RequiredFilesChecker CreateChecker() => new(new RealFileSystem());

    #region ResolveMatchedPaths

    [Test]
    public void ResolveMatchedPaths_SingleInclude_MatchesFiles_ReturnsNonEmpty()
    {
        // Arrange
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            File.WriteAllText(Path.Combine(docDir, "readme.md"), "# Readme");
            File.WriteAllText(Path.Combine(docDir, "contributing.md"), "# Contributing");

            // Act
            var matched = CreateChecker().ResolveMatchedPaths("doc/*.md", tempDir);

            // Assert
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
        // Arrange
        var tempDir = CreateTempDir();
        try
        {
            var docDir = Path.Combine(tempDir, "doc");
            Directory.CreateDirectory(docDir);
            File.WriteAllText(Path.Combine(docDir, "readme.md"), "# Readme");
            File.WriteAllText(Path.Combine(docDir, "contributing.md"), "# Contributing");

            // Act
            var matched = CreateChecker().ResolveMatchedPaths("doc/*.md;!doc/contributing.md", tempDir);

            // Assert
            matched.Should().HaveCount(1);
            matched[0].Should().EndWith("readme.md");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion
}
