using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;

namespace JulianVerdurmen.SlnxValidator.Core.Tests;

public class RealFileSystemIntegrationTests
{
    [Test]
    public async Task RealFileSystem_AllMethods_WorkAgainstDisk()
    {
        var fileSystem = new RealFileSystem();
        var rootDirectory = Path.Combine(Path.GetTempPath(), $"slnx-validator-tests-{Guid.NewGuid():N}");
        var childDirectory = Path.Combine(rootDirectory, "nested");
        var firstFile = Path.Combine(childDirectory, "one.slnx");
        var secondFile = Path.Combine(childDirectory, "two.slnx");

        try
        {
            fileSystem.DirectoryExists(rootDirectory).Should().BeFalse();

            fileSystem.CreateDirectory(childDirectory);

            fileSystem.DirectoryExists(rootDirectory).Should().BeTrue();
            fileSystem.DirectoryExists(childDirectory).Should().BeTrue();

            await using (var stream = fileSystem.CreateFile(firstFile))
            {
                await stream.WriteAsync("<Solution />"u8.ToArray());
            }

            await File.WriteAllTextAsync(secondFile, "<Solution />");

            fileSystem.FileExists(firstFile).Should().BeTrue();
            fileSystem.FileExists(secondFile).Should().BeTrue();
            fileSystem.FileExists(Path.Combine(childDirectory, "missing.slnx")).Should().BeFalse();

            fileSystem.GetFiles(childDirectory, "*.slnx")
                .Select(Path.GetFileName)
                .Should().BeEquivalentTo(["one.slnx", "two.slnx"]);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
                Directory.Delete(rootDirectory, recursive: true);
        }
    }
}