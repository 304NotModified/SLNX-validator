using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Tests;

public class SonarReporterTests
{
    private static SonarReporter CreateReporter() => new(new MockFileSystem());

    private static async Task<JsonDocument> WriteAndReadReportAsync(IReadOnlyList<FileValidationResult> results)
    {
        using var stream = new MemoryStream();
        await CreateReporter().WriteReportAsync(results, stream);
        return JsonDocument.Parse(stream.ToArray());
    }

    [Test]
    public async Task WriteReportAsync_NoErrors_WritesEmptyArrays()
    {
        var results = new List<FileValidationResult>
        {
            new() { File = "test.slnx", HasErrors = false, Errors = [] }
        };

        using var doc = await WriteAndReadReportAsync(results);

        doc.RootElement.GetProperty("rules").GetArrayLength().Should().Be(0);
        doc.RootElement.GetProperty("issues").GetArrayLength().Should().Be(0);
    }

    [Test]
    public async Task WriteReportAsync_WithError_WritesCorrectRuleAndIssue()
    {
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "test.slnx",
                HasErrors = true,
                Errors = [new ValidationError(ValidationErrorCode.ReferencedFileNotFound, "File not found: docs\\README.md")]
            }
        };

        using var doc = await WriteAndReadReportAsync(results);
        var root = doc.RootElement;

        var rule = root.GetProperty("rules")[0];
        rule.GetProperty("id").GetString().Should().Be("SLNX011");
        rule.GetProperty("engineId").GetString().Should().Be("slnx-validator");
        rule.GetProperty("type").GetString().Should().Be("BUG");

        var issue = root.GetProperty("issues")[0];
        issue.GetProperty("ruleId").GetString().Should().Be("SLNX011");
        issue.GetProperty("primaryLocation").GetProperty("filePath").GetString().Should().Be("test.slnx");
        issue.GetProperty("primaryLocation").GetProperty("message").GetString().Should().Be("File not found: docs\\README.md");
    }

    [Test]
    public async Task WriteReportAsync_ErrorWithLine_WritesTextRange()
    {
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "test.slnx",
                HasErrors = true,
                Errors = [new ValidationError(ValidationErrorCode.XsdViolation, "Schema error", Line: 5)]
            }
        };

        using var doc = await WriteAndReadReportAsync(results);

        var textRange = doc.RootElement.GetProperty("issues")[0]
            .GetProperty("primaryLocation")
            .GetProperty("textRange");

        textRange.GetProperty("startLine").GetInt32().Should().Be(5);
    }

    [Test]
    public async Task WriteReportAsync_ErrorWithoutLine_OmitsTextRange()
    {
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "test.slnx",
                HasErrors = true,
                Errors = [new ValidationError(ValidationErrorCode.ReferencedFileNotFound, "File not found: docs\\README.md")]
            }
        };

        using var doc = await WriteAndReadReportAsync(results);

        var primaryLocation = doc.RootElement.GetProperty("issues")[0].GetProperty("primaryLocation");

        primaryLocation.TryGetProperty("textRange", out _).Should().BeFalse();
    }

    [Test]
    public async Task WriteReportAsync_SameErrorCodeTwice_WritesRuleOnce()
    {
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "test.slnx",
                HasErrors = true,
                Errors =
                [
                    new ValidationError(ValidationErrorCode.ReferencedFileNotFound, "File not found: a.md"),
                    new ValidationError(ValidationErrorCode.ReferencedFileNotFound, "File not found: b.md"),
                ]
            }
        };

        using var doc = await WriteAndReadReportAsync(results);
        var root = doc.RootElement;

        root.GetProperty("rules").GetArrayLength().Should().Be(1);
        root.GetProperty("issues").GetArrayLength().Should().Be(2);
    }

    [Test]
    public async Task WriteReportAsync_DifferentErrorCodes_WritesRulePerCode()
    {
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "test.slnx",
                HasErrors = true,
                Errors =
                [
                    new ValidationError(ValidationErrorCode.ReferencedFileNotFound, "File not found: a.md"),
                    new ValidationError(ValidationErrorCode.XsdViolation, "Schema error", Line: 3),
                ]
            }
        };

        using var doc = await WriteAndReadReportAsync(results);
        var root = doc.RootElement;

        root.GetProperty("rules").GetArrayLength().Should().Be(2);
        root.GetProperty("issues").GetArrayLength().Should().Be(2);
    }

    [Test]
    public async Task WriteReportAsync_MatchesSnapshot()
    {
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "Backend.slnx",
                HasErrors = true,
                Errors =
                [
                    new ValidationError(ValidationErrorCode.ReferencedFileNotFound, "File not found: docs\\CONTRIBUTING.md", Line: 4),
                    new ValidationError(ValidationErrorCode.InvalidWildcardUsage, "Wildcard patterns are not supported: docs\\*.md", Line: 8),
                ]
            }
        };

        using var stream = new MemoryStream();
        await CreateReporter().WriteReportAsync(results, stream);

        await Verify(stream, "json");
    }

    [Test]
    public async Task WriteReportAsync_AllErrorCodes_MatchesSnapshot()
    {
        var allCodes = Enum.GetValues<ValidationErrorCode>();
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "Solution.slnx",
                HasErrors = true,
                Errors = [.. allCodes.Select((code, i) => new ValidationError(code, $"Sample message for {code}", Line: i + 1))]
            }
        };

        using var stream = new MemoryStream();
        await CreateReporter().WriteReportAsync(results, stream);

        await Verify(stream, "json");
    }

    [Test]
    public async Task WriteReportAsync_WithOutputPath_CreatesParentDirectory()
    {
        var fileSystem = new MockFileSystem();
        var reporter = new SonarReporter(fileSystem);
        var results = new List<FileValidationResult>
        {
            new() { File = "test.slnx", HasErrors = false, Errors = [] }
        };

        await reporter.WriteReportAsync(results, "output/reports/sonar.json");

        fileSystem.CreatedDirectories.Should().Equal([Path.Combine("output", "reports")]);
    }

    [Test]
    public async Task WriteReportAsync_WithOutputPathNoSubdirectory_DoesNotCreateDirectory()
    {
        var fileSystem = new MockFileSystem();
        var reporter = new SonarReporter(fileSystem);
        var results = new List<FileValidationResult>
        {
            new() { File = "test.slnx", HasErrors = false, Errors = [] }
        };

        await reporter.WriteReportAsync(results, "sonar.json");

        fileSystem.CreatedDirectories.Should().BeEmpty();
    }
}
