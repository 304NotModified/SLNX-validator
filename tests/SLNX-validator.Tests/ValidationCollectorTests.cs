using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;
using NSubstitute;

namespace JulianVerdurmen.SlnxValidator.Tests;

public class ValidationCollectorTests
{
    private static string CreateTempDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static ValidationCollector CreateCollector(
        IRequiredFilesChecker? checker = null,
        ISlnxValidator? validator = null)
    {
        checker ??= Substitute.For<IRequiredFilesChecker>();
        validator ??= Substitute.For<ISlnxValidator>();
        validator
            .ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        return new ValidationCollector(new MockFileSystem(), validator, checker);
    }

    [Test]
    public async Task CollectAsync_RequiredFilesPatternNoMatch_AddsRequiredFileDoesntExistOnSystemError()
    {
        var tempDir = CreateTempDir();
        try
        {
            var slnxPath = Path.Combine(tempDir, "test.slnx");
            await File.WriteAllTextAsync(slnxPath, "<Solution />");

            var fileSystem = new MockFileSystem(slnxPath);
            var validator = Substitute.For<ISlnxValidator>();
            validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult());
            var checker = Substitute.For<IRequiredFilesChecker>();
            var collector = new ValidationCollector(fileSystem, validator, checker);

            // Empty matched paths = pattern matched nothing on disk
            var results = await collector.CollectAsync([slnxPath], matchedRequiredPaths: [], "*.md", CancellationToken.None);

            results.Should().HaveCount(1);
            results[0].HasErrors.Should().BeTrue();
            results[0].Errors.Should().ContainSingle(e => e.Code == ValidationErrorCode.RequiredFileDoesntExistOnSystem);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CollectAsync_RequiredFileMissingFromSlnx_AddsRequiredFileNotReferencedInSolutionError()
    {
        var tempDir = CreateTempDir();
        try
        {
            var slnxPath = Path.Combine(tempDir, "test.slnx");
            await File.WriteAllTextAsync(slnxPath, "<Solution />");
            var requiredFile = Path.Combine(tempDir, "readme.md");

            var fileSystem = new MockFileSystem(slnxPath);
            var validator = Substitute.For<ISlnxValidator>();
            validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult());
            var checker = Substitute.For<IRequiredFilesChecker>();
            checker.CheckInSlnx(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns([new ValidationError(ValidationErrorCode.RequiredFileNotReferencedInSolution,
                    $"Required file is not referenced in the solution: {requiredFile} — add: <File Path=\"readme.md\" />")]);

            var collector = new ValidationCollector(fileSystem, validator, checker);

            var results = await collector.CollectAsync([slnxPath], [requiredFile], "*.md", CancellationToken.None);

            results.Should().HaveCount(1);
            results[0].HasErrors.Should().BeTrue();
            results[0].Errors.Should().ContainSingle(e => e.Code == ValidationErrorCode.RequiredFileNotReferencedInSolution);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CollectAsync_RequiredFileMatchedAndReferenced_NoExtraErrors()
    {
        var tempDir = CreateTempDir();
        try
        {
            var slnxPath = Path.Combine(tempDir, "test.slnx");
            await File.WriteAllTextAsync(slnxPath, "<Solution />");
            var requiredFile = Path.Combine(tempDir, "readme.md");

            var fileSystem = new MockFileSystem(slnxPath);
            var validator = Substitute.For<ISlnxValidator>();
            validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult());
            var checker = Substitute.For<IRequiredFilesChecker>();
            checker.CheckInSlnx(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Array.Empty<ValidationError>());

            var collector = new ValidationCollector(fileSystem, validator, checker);

            var results = await collector.CollectAsync([slnxPath], [requiredFile], "*.md", CancellationToken.None);

            results.Should().HaveCount(1);
            results[0].HasErrors.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CollectAsync_NullMatchedRequiredPaths_SkipsRequiredFilesCheck()
    {
        var tempDir = CreateTempDir();
        try
        {
            var slnxPath = Path.Combine(tempDir, "test.slnx");
            await File.WriteAllTextAsync(slnxPath, "<Solution />");

            var fileSystem = new MockFileSystem(slnxPath);
            var validator = Substitute.For<ISlnxValidator>();
            validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult());
            var checker = Substitute.For<IRequiredFilesChecker>();
            var collector = new ValidationCollector(fileSystem, validator, checker);

            // null matchedRequiredPaths = no --required-files option was provided
            var results = await collector.CollectAsync([slnxPath], matchedRequiredPaths: null, requiredFilesPattern: null, CancellationToken.None);

            results.Should().HaveCount(1);
            results[0].HasErrors.Should().BeFalse();
            checker.DidNotReceive().CheckInSlnx(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(), Arg.Any<string>());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
