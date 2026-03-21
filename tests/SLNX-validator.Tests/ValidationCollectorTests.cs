using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;
using NSubstitute;

namespace JulianVerdurmen.SlnxValidator.Tests;

public class ValidationCollectorTests
{
    private const string SlnxPath = "/fake/test.slnx";
    private const string SlnxContent = "<Solution />";

    private static (ValidationCollector collector, IRequiredFilesChecker checker) CreateCollector(
        string? slnxContent = SlnxContent,
        IRequiredFilesChecker? checker = null)
    {
        checker ??= Substitute.For<IRequiredFilesChecker>();
        var validator = Substitute.For<ISlnxValidator>();
        validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var fileSystem = new MockFileSystem(new Dictionary<string, string>
        {
            [SlnxPath] = slnxContent ?? ""
        });
        return (new ValidationCollector(fileSystem, validator, checker), checker);
    }

    [Test]
    public async Task CollectAsync_RequiredFilesPatternNoMatch_AddsRequiredFileDoesntExistOnSystemError()
    {
        var (collector, _) = CreateCollector();
        var options = new RequiredFilesOptions(MatchedPaths: [], Pattern: "*.md");

        var results = await collector.CollectAsync([SlnxPath], options, CancellationToken.None);

        results.Should().HaveCount(1);
        results[0].HasErrors.Should().BeTrue();
        results[0].Errors.Should().ContainSingle(e => e.Code == ValidationErrorCode.RequiredFileDoesntExistOnSystem);
    }

    [Test]
    public async Task CollectAsync_RequiredFileMissingFromSlnx_AddsRequiredFileNotReferencedInSolutionError()
    {
        var requiredFile = "/fake/doc/readme.md";
        var checker = Substitute.For<IRequiredFilesChecker>();
        checker.CheckInSlnx(Arg.Any<IReadOnlyList<string>>(), Arg.Any<SlnxFileRefs>())
            .Returns([new ValidationError(ValidationErrorCode.RequiredFileNotReferencedInSolution,
                $"Required file is not referenced in the solution: {requiredFile} — add: <File Path=\"doc/readme.md\" />")]);

        var (collector, _) = CreateCollector(checker: checker);
        var options = new RequiredFilesOptions(MatchedPaths: [requiredFile], Pattern: "doc/*.md");

        var results = await collector.CollectAsync([SlnxPath], options, CancellationToken.None);

        results.Should().HaveCount(1);
        results[0].HasErrors.Should().BeTrue();
        results[0].Errors.Should().ContainSingle(e => e.Code == ValidationErrorCode.RequiredFileNotReferencedInSolution);
    }

    [Test]
    public async Task CollectAsync_RequiredFileMatchedAndReferenced_NoExtraErrors()
    {
        var requiredFile = "/fake/doc/readme.md";
        var checker = Substitute.For<IRequiredFilesChecker>();
        checker.CheckInSlnx(Arg.Any<IReadOnlyList<string>>(), Arg.Any<SlnxFileRefs>())
            .Returns(Array.Empty<ValidationError>());

        var (collector, _) = CreateCollector(checker: checker);
        var options = new RequiredFilesOptions(MatchedPaths: [requiredFile], Pattern: "doc/*.md");

        var results = await collector.CollectAsync([SlnxPath], options, CancellationToken.None);

        results.Should().HaveCount(1);
        results[0].HasErrors.Should().BeFalse();
    }

    [Test]
    public async Task CollectAsync_NullOptions_SkipsRequiredFilesCheck()
    {
        var (collector, checker) = CreateCollector();

        var results = await collector.CollectAsync([SlnxPath], requiredFilesOptions: null, CancellationToken.None);

        results.Should().HaveCount(1);
        results[0].HasErrors.Should().BeFalse();
        checker.DidNotReceive().CheckInSlnx(Arg.Any<IReadOnlyList<string>>(), Arg.Any<SlnxFileRefs>());
    }
}

