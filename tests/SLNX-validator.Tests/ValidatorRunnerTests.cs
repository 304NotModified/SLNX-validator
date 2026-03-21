using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using NSubstitute;
namespace JulianVerdurmen.SlnxValidator.Tests;

public class ValidatorRunnerTests
{
    private static ValidatorRunner CreateRunner(IFileSystem fileSystem, IRequiredFilesChecker? checker = null)
    {
        checker ??= Substitute.For<IRequiredFilesChecker>();
        var collector = new ValidationCollector(fileSystem, Substitute.For<ISlnxValidator>(), checker);
        var sonarReporter = new SonarReporter(fileSystem);
        return new ValidatorRunner(Substitute.For<ISlnxFileResolver>(), collector, sonarReporter, checker);
    }

    [Test]
    public async Task RunAsync_FileNotFound_ContinueOnErrorFalse_ReturnsOne()
    {
        var runner = CreateRunner(new MockFileSystem());

        var exitCode = await runner.RunAsync("nonexistent.slnx", sonarqubeReportPath: null, continueOnError: false,
            requiredFilesPattern: null, workingDirectory: ".", CancellationToken.None);

        exitCode.Should().Be(1);
    }

    [Test]
    public async Task RunAsync_FileNotFound_ContinueOnErrorTrue_ReturnsZero()
    {
        var runner = CreateRunner(new MockFileSystem());

        var exitCode = await runner.RunAsync("nonexistent.slnx", sonarqubeReportPath: null, continueOnError: true,
            requiredFilesPattern: null, workingDirectory: ".", CancellationToken.None);

        exitCode.Should().Be(0);
    }

    [Test]
    public async Task RunAsync_NoFilesFound_ContinueOnErrorFalse_ReturnsOne()
    {
        var runner = CreateRunner(new MockFileSystem());

        var exitCode = await runner.RunAsync("src/*.slnx", sonarqubeReportPath: null, continueOnError: false,
            requiredFilesPattern: null, workingDirectory: ".", CancellationToken.None);

        exitCode.Should().Be(1);
    }

    [Test]
    public async Task RunAsync_NoFilesFound_ContinueOnErrorTrue_ReturnsZero()
    {
        var runner = CreateRunner(new MockFileSystem());

        var exitCode = await runner.RunAsync("src/*.slnx", sonarqubeReportPath: null, continueOnError: true,
            requiredFilesPattern: null, workingDirectory: ".", CancellationToken.None);

        exitCode.Should().Be(0);
    }
}

