using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.SarifReporting;
using JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;
using NSubstitute;
namespace JulianVerdurmen.SlnxValidator.Tests;

public class ValidatorRunnerTests
{
    private static ValidatorRunner CreateRunner(IFileSystem fileSystem, IRequiredFilesChecker? checker = null, IConsole? console = null)
    {
        checker ??= Substitute.For<IRequiredFilesChecker>();
        var resolver = Substitute.For<ISlnxFileResolver>();
        var collector = new SlnxCollector(fileSystem, resolver, Substitute.For<ISlnxValidator>(), checker);
        var sonarReporter = new SonarReporter(fileSystem);
        var sarifReporter = new SarifReporter(fileSystem);
        return new ValidatorRunner(collector, sonarReporter, sarifReporter, checker, fileSystem, console ?? new TestConsole());
    }

    private static ValidatorRunnerOptions Options(string input = "test.slnx",
        bool continueOnError = false, string? requiredFilesPattern = null) =>
        new(input, SonarqubeReportPath: null, continueOnError, requiredFilesPattern, WorkingDirectory: ".");

    private static ValidatorRunner CreateRunnerWithSlnx(
        string slnxPath, string slnxContent, IRequiredFilesChecker? checker = null, IConsole? console = null)
    {
        checker ??= Substitute.For<IRequiredFilesChecker>();
        var fileSystem = new MockFileSystem(new Dictionary<string, string>
        {
            [slnxPath] = slnxContent
        });
        var validator = Substitute.For<ISlnxValidator>();
        validator.ValidateAsync(Arg.Any<SlnxFile>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var resolver = Substitute.For<ISlnxFileResolver>();
        resolver.Resolve(Arg.Any<string>()).Returns([slnxPath]);
        var collector = new SlnxCollector(fileSystem, resolver, validator, checker);
        var sonarReporter = new SonarReporter(fileSystem);
        var sarifReporter = new SarifReporter(fileSystem);
        return new ValidatorRunner(collector, sonarReporter, sarifReporter, checker, fileSystem, console ?? new TestConsole());
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

    #region RunAsync – severity overrides

    // All severity override tests use a .xml file (not .slnx) to generate SLNX002 (InvalidExtension) errors,
    // which allows testing severity override behavior with predictable validation output.

    [Test]
    public async Task RunAsync_IgnoreAllCodes_WithErrors_ReturnsZero()
    {
        // Arrange: file with wrong extension generates SLNX002; --ignore * suppresses all codes
        var runner = CreateRunnerWithSlnx("test.xml", "<Solution />");
        var overrides = SeverityOverridesParser.Parse(null, null, null, null, null, ignore: "*");

        // Act
        var exitCode = await runner.RunAsync(
            new ValidatorRunnerOptions("test.xml", null, false, null, ".", overrides),
            CancellationToken.None);

        // Assert
        exitCode.Should().Be(0);
    }

    [Test]
    public async Task RunAsync_IgnoreSpecificCode_ThatCodeDoesNotCauseExitOne()
    {
        // Arrange: --ignore SLNX002 suppresses the InvalidExtension error
        var runner = CreateRunnerWithSlnx("test.xml", "<Solution />");
        var overrides = SeverityOverridesParser.Parse(null, null, null, null, null, ignore: "SLNX002");

        // Act
        var exitCode = await runner.RunAsync(
            new ValidatorRunnerOptions("test.xml", null, false, null, ".", overrides),
            CancellationToken.None);

        // Assert
        exitCode.Should().Be(0);
    }

    [Test]
    public async Task RunAsync_MinorOverrideForErrorCode_ReturnsZero()
    {
        // Arrange: --minor SLNX002 downgrades InvalidExtension to non-failing severity
        var runner = CreateRunnerWithSlnx("test.xml", "<Solution />");
        var overrides = SeverityOverridesParser.Parse(null, null, null, minor: "SLNX002", null, null);

        // Act
        var exitCode = await runner.RunAsync(
            new ValidatorRunnerOptions("test.xml", null, false, null, ".", overrides),
            CancellationToken.None);

        // Assert
        exitCode.Should().Be(0);
    }

    [Test]
    public async Task RunAsync_InfoAllCodes_ReturnsZero()
    {
        // Arrange: --info * downgrades all codes to INFO (non-failing)
        var runner = CreateRunnerWithSlnx("test.xml", "<Solution />");
        var overrides = SeverityOverridesParser.Parse(null, null, null, null, info: "*", null);

        // Act
        var exitCode = await runner.RunAsync(
            new ValidatorRunnerOptions("test.xml", null, false, null, ".", overrides),
            CancellationToken.None);

        // Assert
        exitCode.Should().Be(0);
    }

    [Test]
    public async Task RunAsync_InfoAllCodesMajorSpecificCode_SpecificCodeCausesExitOne()
    {
        // Arrange: --info * --major SLNX002  →  SLNX002 stays MAJOR (specific overrides wildcard)
        var runner = CreateRunnerWithSlnx("test.xml", "<Solution />");
        var overrides = SeverityOverridesParser.Parse(null, null, major: "SLNX002", null, info: "*", null);

        // Act
        var exitCode = await runner.RunAsync(
            new ValidatorRunnerOptions("test.xml", null, false, null, ".", overrides),
            CancellationToken.None);

        // Assert
        exitCode.Should().Be(1);
    }

    [Test]
    public async Task RunAsync_IgnoreAllCodesMajorSpecificCode_SpecificCodeCausesExitOne()
    {
        // Arrange: --ignore * --major SLNX002  →  SLNX002 is MAJOR (specific wins over wildcard ignore)
        var runner = CreateRunnerWithSlnx("test.xml", "<Solution />");
        var overrides = SeverityOverridesParser.Parse(null, null, major: "SLNX002", null, null, ignore: "*");

        // Act
        var exitCode = await runner.RunAsync(
            new ValidatorRunnerOptions("test.xml", null, false, null, ".", overrides),
            CancellationToken.None);

        // Assert
        exitCode.Should().Be(1);
    }

    #endregion

    #region RunAsync – console output

    [Test]
    public async Task RunAsync_NoFilesFound_WritesErrorToConsole()
    {
        // Arrange
        var console = new TestConsole();
        var runner = CreateRunner(new MockFileSystem(), console: console);

        // Act
        await runner.RunAsync(Options("nonexistent.slnx"), CancellationToken.None);

        // Assert
        console.Error.ToString().Should().Contain("No .slnx files found for input: nonexistent.slnx");
    }

    [Test]
    public async Task RunAsync_SonarqubeReportPath_WritesConfirmationToConsole()
    {
        // Arrange
        var slnxPath = Path.GetFullPath("test.slnx");
        var console = new TestConsole();
        var runner = CreateRunnerWithSlnx(slnxPath, "<Solution />", console: console);
        var options = new ValidatorRunnerOptions(slnxPath, SonarqubeReportPath: "report.xml",
            ContinueOnError: false, RequiredFilesPattern: null, WorkingDirectory: ".");

        // Act
        await runner.RunAsync(options, CancellationToken.None);

        // Assert
        console.Out.ToString().Should().Contain("SonarQube report written to: report.xml");
    }

    [Test]
    public async Task RunAsync_SarifReportPath_WritesConfirmationToConsole()
    {
        // Arrange
        var slnxPath = Path.GetFullPath("test.slnx");
        var console = new TestConsole();
        var runner = CreateRunnerWithSlnx(slnxPath, "<Solution />", console: console);
        var options = new ValidatorRunnerOptions(slnxPath, SonarqubeReportPath: null,
            ContinueOnError: false, RequiredFilesPattern: null, WorkingDirectory: ".",
            SarifReportPath: "report.sarif");

        // Act
        await runner.RunAsync(options, CancellationToken.None);

        // Assert
        console.Out.ToString().Should().Contain("SARIF report written to: report.sarif");
    }

    #endregion
}

