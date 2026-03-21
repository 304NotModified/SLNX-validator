using AwesomeAssertions;

namespace JulianVerdurmen.SlnxValidator.Tests;

public class ProgramIntegrationTests
{
    [Test]
    public async Task Invoke_WithNoArguments_ReturnsNonZeroExitCode()
    {
        var exitCode = await Program.Main([]);

        exitCode.Should().NotBe(0);
    }

    [Test]
    public async Task Invoke_WithNonExistentFile_ReturnsNonZeroExitCode()
    {
        var exitCode = await Program.Main(["C:\\does\\not\\exist.slnx"]);

        exitCode.Should().NotBe(0);
    }

    [Test]
    public async Task Invoke_WithDirectory_ValidatesAllSlnxFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        var csprojPath = Path.Combine(tempDir, "App.csproj");
        var slnxPath = Path.Combine(tempDir, "test.slnx");

        await File.WriteAllTextAsync(csprojPath, "<Project />");
        await File.WriteAllTextAsync(slnxPath, """
            <Solution>
              <Project Path="App.csproj" />
            </Solution>
            """);

        try
        {
            var exitCode = await Program.Main([tempDir]);

            exitCode.Should().Be(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Invoke_WithGlobPattern_ValidatesMatchingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        var csprojPath = Path.Combine(tempDir, "App.csproj");
        var slnxPath = Path.Combine(tempDir, "test.slnx");

        await File.WriteAllTextAsync(csprojPath, "<Project />");
        await File.WriteAllTextAsync(slnxPath, """
            <Solution>
              <Project Path="App.csproj" />
            </Solution>
            """);

        try
        {
            var exitCode = await Program.Main([$"{tempDir}/*.slnx"]);

            exitCode.Should().Be(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Invoke_WithValidSlnxFile_ReturnsZeroExitCode()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        var csprojPath = Path.Combine(tempDir, "App.csproj");
        var slnxPath = Path.Combine(tempDir, "test.slnx");

        await File.WriteAllTextAsync(csprojPath, "<Project />");
        await File.WriteAllTextAsync(slnxPath, """
            <Solution>
              <Project Path="App.csproj" />
            </Solution>
            """);

        try
        {
            var exitCode = await Program.Main([slnxPath]);

            exitCode.Should().Be(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Invoke_WithNonSlnxExtension_ReturnsNonZeroExitCode()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".xml");
        await File.WriteAllTextAsync(path, "<Solution />");

        try
        {
            var exitCode = await Program.Main([path]);

            exitCode.Should().NotBe(0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task Invoke_WithBinaryFile_ReturnsNonZeroExitCode()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".slnx");
        await File.WriteAllBytesAsync(path, [0x00, 0x01, 0x02, 0xFF, 0xFE]);

        try
        {
            var exitCode = await Program.Main([path]);

            exitCode.Should().NotBe(0);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
