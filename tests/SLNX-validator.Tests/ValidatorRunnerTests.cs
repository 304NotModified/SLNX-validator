using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;
using NSubstitute;
namespace JulianVerdurmen.SlnxValidator.Tests;

public class ValidatorRunnerTests
{
    private static ValidatorRunner CreateRunner(IFileSystem fileSystem, IRequiredFilesChecker? checker = null)
    {
        checker ??= Substitute.For<IRequiredFilesChecker>();
        var collector = new ValidationCollector(fileSystem, Substitute.For<ISlnxValidator>(), checker);
        var sonarReporter = new SonarReporter(fileSystem);
        return new ValidatorRunner(Substitute.For<ISlnxFileResolver>(), collector, sonarReporter, checker, fileSystem);
    }

    private static ValidatorRunnerOptions Options(string input = "test.slnx",
        bool continueOnError = false, string? requiredFilesPattern = null) =>
        new(input, SonarqubeReportPath: null, continueOnError, requiredFilesPattern, WorkingDirectory: ".");

    private static ValidatorRunner CreateRunnerWithSlnx(
        string slnxPath, string slnxContent, IRequiredFilesChecker? checker = null)
    {
        checker ??= Substitute.For<IRequiredFilesChecker>();
        var fileSystem = new MockFileSystem(new Dictionary<string, string>
        {
            [slnxPath] = slnxContent
        });
        var validator = Substitute.For<ISlnxValidator>();
        validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var collector = new ValidationCollector(fileSystem, validator, checker);
        var sonarReporter = new SonarReporter(fileSystem);
        var resolver = Substitute.For<ISlnxFileResolver>();
        resolver.Resolve(Arg.Any<string>()).Returns([slnxPath]);
        return new ValidatorRunner(resolver, collector, sonarReporter, checker, fileSystem);
    }

    #region RunAsync – file resolution

    [Test]
    public async Task RunAsync_FileNotFound_ContinueOnErrorFalse_ReturnsOne()
    {
        // Arrange
        var runner = CreateRunner(new MockFileSystem());

        // Act
        var exitCode = await runner.RunAsync(Options("nonexistent.slnx"), CancellationToken.None);

        // Assert
        exitCode.Should().Be(1);
    }

    [Test]
    public async Task RunAsync_FileNotFound_ContinueOnErrorTrue_ReturnsZero()
    {
        // Arrange
        var runner = CreateRunner(new MockFileSystem());

        // Act
        var exitCode = await runner.RunAsync(Options("nonexistent.slnx", continueOnError: true), CancellationToken.None);

        // Assert
        exitCode.Should().Be(0);
    }

    [Test]
    public async Task RunAsync_NoFilesFound_ContinueOnErrorFalse_ReturnsOne()
    {
        // Arrange
        var runner = CreateRunner(new MockFileSystem());

        // Act
        var exitCode = await runner.RunAsync(Options("src/*.slnx"), CancellationToken.None);

        // Assert
        exitCode.Should().Be(1);
    }

    [Test]
    public async Task RunAsync_NoFilesFound_ContinueOnErrorTrue_ReturnsZero()
    {
        // Arrange
        var runner = CreateRunner(new MockFileSystem());

        // Act
        var exitCode = await runner.RunAsync(Options("src/*.slnx", continueOnError: true), CancellationToken.None);

        // Assert
        exitCode.Should().Be(0);
    }

    #endregion

    #region RunAsync – --required-files

    [Test]
    public async Task RunAsync_RequiredFiles_AllMatchedAndReferenced_ReturnsZero()
    {
        // Arrange
        var slnxPath = Path.GetFullPath("test.slnx");
        var slnxDir = Path.GetDirectoryName(slnxPath)!;
        var requiredFile = Path.GetFullPath(Path.Combine(slnxDir, "doc", "readme.md"));

        var checker = Substitute.For<IRequiredFilesChecker>();
        checker.ResolveMatchedPaths(Arg.Any<string>(), Arg.Any<string>())
            .Returns([requiredFile]);
        checker.CheckInSlnx(Arg.Any<IReadOnlyList<string>>(), Arg.Any<SlnxFile>())
            .Returns([]);

        var runner = CreateRunnerWithSlnx(slnxPath, "<Solution />", checker);

        // Act
        var exitCode = await runner.RunAsync(
            Options(slnxPath, requiredFilesPattern: "doc/*.md"), CancellationToken.None);

        // Assert
        exitCode.Should().Be(0);
    }

    [Test]
    public async Task RunAsync_RequiredFiles_NoMatchOnDisk_ReturnsOne()
    {
        // Arrange
        var slnxPath = Path.GetFullPath("test.slnx");

        var checker = Substitute.For<IRequiredFilesChecker>();
        checker.ResolveMatchedPaths(Arg.Any<string>(), Arg.Any<string>())
            .Returns([]); // nothing matched on disk

        var runner = CreateRunnerWithSlnx(slnxPath, "<Solution />", checker);

        // Act
        var exitCode = await runner.RunAsync(
            Options(slnxPath, requiredFilesPattern: "nonexistent/**/*.md"), CancellationToken.None);

        // Assert
        exitCode.Should().Be(1);
    }

    #endregion
}

