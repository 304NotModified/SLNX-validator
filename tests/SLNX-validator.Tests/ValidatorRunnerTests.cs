using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using CoreSlnxValidator = JulianVerdurmen.SlnxValidator.Core.Validation.SlnxValidator;

namespace JulianVerdurmen.SlnxValidator.Tests;

public class ValidatorRunnerTests
{
    private static ValidatorRunner CreateRunner(IFileSystem fileSystem)
    {
        var resolver = new SlnxFileResolver(fileSystem);
        var validator = new CoreSlnxValidator(fileSystem, new XsdValidator());
        var collector = new ValidationCollector(fileSystem, validator);
        return new ValidatorRunner(resolver, collector);
    }

    [Test]
    public async Task RunAsync_FileNotFound_ContinueOnErrorFalse_ReturnsOne()
    {
        var runner = CreateRunner(new MockFileSystem());

        var exitCode = await runner.RunAsync("nonexistent.slnx", sonarqubeReportPath: null, continueOnError: false, CancellationToken.None);

        exitCode.Should().Be(1);
    }

    [Test]
    public async Task RunAsync_FileNotFound_ContinueOnErrorTrue_ReturnsZero()
    {
        var runner = CreateRunner(new MockFileSystem());

        var exitCode = await runner.RunAsync("nonexistent.slnx", sonarqubeReportPath: null, continueOnError: true, CancellationToken.None);

        exitCode.Should().Be(0);
    }

    [Test]
    public async Task RunAsync_NoFilesFound_ContinueOnErrorFalse_ReturnsOne()
    {
        var runner = CreateRunner(new MockFileSystem());

        var exitCode = await runner.RunAsync("src/*.slnx", sonarqubeReportPath: null, continueOnError: false, CancellationToken.None);

        exitCode.Should().Be(1);
    }

    [Test]
    public async Task RunAsync_NoFilesFound_ContinueOnErrorTrue_ReturnsZero()
    {
        var runner = CreateRunner(new MockFileSystem());

        var exitCode = await runner.RunAsync("src/*.slnx", sonarqubeReportPath: null, continueOnError: true, CancellationToken.None);

        exitCode.Should().Be(0);
    }
}
