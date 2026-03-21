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
        return new ValidatorRunner(Substitute.For<ISlnxFileResolver>(), collector, sonarReporter, checker);
    }

    private static ValidatorRunnerOptions Options(string input = "test.slnx",
        bool continueOnError = false, string? requiredFilesPattern = null) =>
        new(input, SonarqubeReportPath: null, continueOnError, requiredFilesPattern, WorkingDirectory: ".");

    [Test]
    public async Task RunAsync_FileNotFound_ContinueOnErrorFalse_ReturnsOne()
    {
        var runner = CreateRunner(new MockFileSystem());

        var exitCode = await runner.RunAsync(Options("nonexistent.slnx"), CancellationToken.None);

        exitCode.Should().Be(1);
    }

    [Test]
    public async Task RunAsync_FileNotFound_ContinueOnErrorTrue_ReturnsZero()
    {
        var runner = CreateRunner(new MockFileSystem());

        var exitCode = await runner.RunAsync(Options("nonexistent.slnx", continueOnError: true), CancellationToken.None);

        exitCode.Should().Be(0);
    }

    [Test]
    public async Task RunAsync_NoFilesFound_ContinueOnErrorFalse_ReturnsOne()
    {
        var runner = CreateRunner(new MockFileSystem());

        var exitCode = await runner.RunAsync(Options("src/*.slnx"), CancellationToken.None);

        exitCode.Should().Be(1);
    }

    [Test]
    public async Task RunAsync_NoFilesFound_ContinueOnErrorTrue_ReturnsZero()
    {
        var runner = CreateRunner(new MockFileSystem());

        var exitCode = await runner.RunAsync(Options("src/*.slnx", continueOnError: true), CancellationToken.None);

        exitCode.Should().Be(0);
    }

    // ── --required-files unit tests ───────────────────────────────────────────

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
        return new ValidatorRunner(resolver, collector, sonarReporter, checker);
    }

    [Test]
    public async Task RunAsync_RequiredFiles_AllMatchedAndReferenced_ReturnsZero()
    {
        var slnxPath = Path.GetFullPath("test.slnx");
        var slnxDir = Path.GetDirectoryName(slnxPath)!;
        var requiredFile = Path.GetFullPath(Path.Combine(slnxDir, "doc", "readme.md"));

        var checker = Substitute.For<IRequiredFilesChecker>();
        checker.ResolveMatchedPaths(Arg.Any<string>(), Arg.Any<string>())
            .Returns([requiredFile]);
        checker.CheckInSlnx(Arg.Any<IReadOnlyList<string>>(), Arg.Any<SlnxFileRefs>())
            .Returns([]);

        var runner = CreateRunnerWithSlnx(slnxPath, "<Solution />", checker);

        var exitCode = await runner.RunAsync(
            Options(slnxPath, requiredFilesPattern: "doc/*.md"), CancellationToken.None);

        exitCode.Should().Be(0);
    }

    [Test]
    public async Task RunAsync_RequiredFiles_NoMatchOnDisk_ReturnsOne()
    {
        var slnxPath = Path.GetFullPath("test.slnx");

        var checker = Substitute.For<IRequiredFilesChecker>();
        checker.ResolveMatchedPaths(Arg.Any<string>(), Arg.Any<string>())
            .Returns([]); // nothing matched on disk

        var runner = CreateRunnerWithSlnx(slnxPath, "<Solution />", checker);

        var exitCode = await runner.RunAsync(
            Options(slnxPath, requiredFilesPattern: "nonexistent/**/*.md"), CancellationToken.None);

        exitCode.Should().Be(1);
    }
}

