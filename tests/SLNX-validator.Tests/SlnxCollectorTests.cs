using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;
using NSubstitute;

namespace JulianVerdurmen.SlnxValidator.Tests;

public class SlnxCollectorTests
{
    private const string SlnxPath = "/fake/test.slnx";
    private const string SlnxContent = "<Solution />";

    private static (SlnxCollector collector, IRequiredFilesChecker checker) CreateCollector(
        string? slnxContent = SlnxContent,
        IRequiredFilesChecker? checker = null)
    {
        checker ??= Substitute.For<IRequiredFilesChecker>();
        var validator = Substitute.For<ISlnxValidator>();
        validator.ValidateAsync(Arg.Any<SlnxFile>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var fileSystem = new MockFileSystem(new Dictionary<string, string>
        {
            [SlnxPath] = slnxContent ?? ""
        });
        var resolver = Substitute.For<ISlnxFileResolver>();
        resolver.Resolve(Arg.Any<string>()).Returns([SlnxPath]);
        return (new SlnxCollector(fileSystem, resolver, validator, checker), checker);
    }

    #region CollectAsync

    [Test]
    public async Task CollectAsync_RequiredFilesPatternNoMatch_NoError()
    {
        // Arrange
        var (collector, _) = CreateCollector();
        var options = new RequiredFilesOptions(MatchedPaths: [], Pattern: "*.md");

        // Act
        var results = await collector.CollectAsync(SlnxPath, options, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results[0].HasErrors.Should().BeFalse();
    }

    [Test]
    public async Task CollectAsync_RequiredFileMissingFromSlnx_AddsRequiredFileNotReferencedInSolutionError()
    {
        // Arrange
        var requiredFile = "/fake/doc/readme.md";
        var checker = Substitute.For<IRequiredFilesChecker>();
        checker.CheckInSlnx(Arg.Any<IReadOnlyList<string>>(), Arg.Any<SlnxFile>())
            .Returns([new ValidationError(ValidationErrorCode.RequiredFileNotReferencedInSolution,
                $"Required file is not referenced in the solution: {requiredFile} — add: <File Path=\"doc/readme.md\" />")]);

        var (collector, _) = CreateCollector(checker: checker);
        var options = new RequiredFilesOptions(MatchedPaths: [requiredFile], Pattern: "doc/*.md");

        // Act
        var results = await collector.CollectAsync(SlnxPath, options, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results[0].HasErrors.Should().BeTrue();
        results[0].Errors.Should().ContainSingle(e => e.Code == ValidationErrorCode.RequiredFileNotReferencedInSolution);
    }

    [Test]
    public async Task CollectAsync_RequiredFileMatchedAndReferenced_NoExtraErrors()
    {
        // Arrange
        var requiredFile = "/fake/doc/readme.md";
        var checker = Substitute.For<IRequiredFilesChecker>();
        checker.CheckInSlnx(Arg.Any<IReadOnlyList<string>>(), Arg.Any<SlnxFile>())
            .Returns(Array.Empty<ValidationError>());

        var (collector, _) = CreateCollector(checker: checker);
        var options = new RequiredFilesOptions(MatchedPaths: [requiredFile], Pattern: "doc/*.md");

        // Act
        var results = await collector.CollectAsync(SlnxPath, options, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results[0].HasErrors.Should().BeFalse();
    }

    [Test]
    public async Task CollectAsync_NullOptions_SkipsRequiredFilesCheck()
    {
        // Arrange
        var (collector, checker) = CreateCollector();

        // Act
        var results = await collector.CollectAsync(SlnxPath, requiredFilesOptions: null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results[0].HasErrors.Should().BeFalse();
        checker.DidNotReceive().CheckInSlnx(Arg.Any<IReadOnlyList<string>>(), Arg.Any<SlnxFile>());
    }

    [Test]
    public async Task CollectAsync_FileNotFound_UsesFullMessageAndShortMessage()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var validator = Substitute.For<ISlnxValidator>();
        var checker = Substitute.For<IRequiredFilesChecker>();
        var resolver = Substitute.For<ISlnxFileResolver>();
        resolver.Resolve(Arg.Any<string>()).Returns([SlnxPath]);
        var collector = new SlnxCollector(fileSystem, resolver, validator, checker);

        // Act
        var results = await collector.CollectAsync(SlnxPath, requiredFilesOptions: null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results[0].HasErrors.Should().BeTrue();
        var error = results[0].Errors.Should().ContainSingle(e => e.Code == ValidationErrorCode.FileNotFound).Which;
        error.Message.Should().Be($"File not found: {SlnxPath}");
        error.ShortMessage.Should().Be("The specified .slnx file does not exist");
    }

    [Test]
    public async Task CollectAsync_InvalidXml_ReturnsInvalidXmlError()
    {
        // Arrange
        var fileSystem = new MockFileSystem(new Dictionary<string, string>
        {
            [SlnxPath] = "this is not xml at all"
        });
        var validator = Substitute.For<ISlnxValidator>();
        var checker = Substitute.For<IRequiredFilesChecker>();
        var resolver = Substitute.For<ISlnxFileResolver>();
        resolver.Resolve(Arg.Any<string>()).Returns([SlnxPath]);
        var collector = new SlnxCollector(fileSystem, resolver, validator, checker);

        // Act
        var results = await collector.CollectAsync(SlnxPath, requiredFilesOptions: null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results[0].HasErrors.Should().BeTrue();
        results[0].Errors.Should().ContainSingle(e => e.Code == ValidationErrorCode.InvalidXml);
        results[0].Errors[0].Message.Should().Contain("Invalid XML");
        await validator.DidNotReceive().ValidateAsync(Arg.Any<SlnxFile>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
