using System.Text.Json;
using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.Reporting;
using JulianVerdurmen.SlnxValidator.Core.SarifReporting;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Tests;

public class SarifReporterTests
{
    private static SarifReporter CreateReporter() => new(new MockFileSystem());

    private static async Task<JsonDocument> WriteAndReadReportAsync(
        IReadOnlyList<FileValidationResult> results,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? severityOverrides = null)
    {
        using var stream = new MemoryStream();
        await CreateReporter().WriteReportAsync(results, stream, severityOverrides);
        return JsonDocument.Parse(stream.ToArray());
    }

    [Test]
    public async Task WriteReportAsync_NoErrors_WritesEmptyArrays()
    {
        // Arrange
        var results = new List<FileValidationResult>
        {
            new() { File = "test.slnx", HasErrors = false, Errors = [] }
        };

        // Act
        using var doc = await WriteAndReadReportAsync(results);

        // Assert
        var run = doc.RootElement.GetProperty("runs")[0];
        run.GetProperty("tool").GetProperty("driver").GetProperty("rules").GetArrayLength().Should().Be(0);
        run.GetProperty("results").GetArrayLength().Should().Be(0);
    }

    [Test]
    public async Task WriteReportAsync_ContainsSarifSchemaAndVersion()
    {
        // Arrange
        var results = new List<FileValidationResult>
        {
            new() { File = "test.slnx", HasErrors = false, Errors = [] }
        };

        // Act
        using var doc = await WriteAndReadReportAsync(results);

        // Assert
        doc.RootElement.GetProperty("$schema").GetString()
            .Should().Be("https://json.schemastore.org/sarif-2.1.0.json");
        doc.RootElement.GetProperty("version").GetString().Should().Be("2.1.0");
    }

    [Test]
    public async Task WriteReportAsync_ToolDriverName_IsSlnxValidator()
    {
        // Arrange
        var results = new List<FileValidationResult>
        {
            new() { File = "test.slnx", HasErrors = false, Errors = [] }
        };

        // Act
        using var doc = await WriteAndReadReportAsync(results);

        // Assert
        doc.RootElement.GetProperty("runs")[0]
            .GetProperty("tool").GetProperty("driver").GetProperty("name").GetString()
            .Should().Be("slnx-validator");
    }

    [Test]
    public async Task WriteReportAsync_WithError_WritesCorrectRuleAndResult()
    {
        // Arrange
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "test.slnx",
                HasErrors = true,
                Errors = [new ValidationError(ValidationErrorCode.ReferencedFileNotFound, "File not found: docs\\README.md")]
            }
        };

        // Act
        using var doc = await WriteAndReadReportAsync(results);
        var run = doc.RootElement.GetProperty("runs")[0];

        // Assert
        var rule = run.GetProperty("tool").GetProperty("driver").GetProperty("rules")[0];
        rule.GetProperty("id").GetString().Should().Be("SLNX011");
        rule.GetProperty("shortDescription").GetProperty("text").GetString().Should().Be("Referenced file not found");
        rule.GetProperty("defaultConfiguration").GetProperty("level").GetString().Should().Be("error");

        var result = run.GetProperty("results")[0];
        result.GetProperty("ruleId").GetString().Should().Be("SLNX011");
        result.GetProperty("level").GetString().Should().Be("error");
        result.GetProperty("message").GetProperty("text").GetString().Should().Be("File not found: docs\\README.md");
        result.GetProperty("locations")[0]
            .GetProperty("physicalLocation").GetProperty("artifactLocation").GetProperty("uri").GetString()
            .Should().Be("test.slnx");
    }

    [Test]
    public async Task WriteReportAsync_ErrorWithLine_WritesRegion()
    {
        // Arrange
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "test.slnx",
                HasErrors = true,
                Errors = [new ValidationError(ValidationErrorCode.XsdViolation, "Schema error", Line: 5)]
            }
        };

        // Act
        using var doc = await WriteAndReadReportAsync(results);

        // Assert
        var region = doc.RootElement.GetProperty("runs")[0]
            .GetProperty("results")[0]
            .GetProperty("locations")[0]
            .GetProperty("physicalLocation")
            .GetProperty("region");
        region.GetProperty("startLine").GetInt32().Should().Be(5);
    }

    [Test]
    public async Task WriteReportAsync_ErrorWithoutLine_OmitsRegion()
    {
        // Arrange
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "test.slnx",
                HasErrors = true,
                Errors = [new ValidationError(ValidationErrorCode.ReferencedFileNotFound, "File not found: docs\\README.md")]
            }
        };

        // Act
        using var doc = await WriteAndReadReportAsync(results);

        // Assert
        var physicalLocation = doc.RootElement.GetProperty("runs")[0]
            .GetProperty("results")[0]
            .GetProperty("locations")[0]
            .GetProperty("physicalLocation");
        physicalLocation.TryGetProperty("region", out _).Should().BeFalse();
    }

    [Test]
    public async Task WriteReportAsync_SameErrorCodeTwice_WritesRuleOnce()
    {
        // Arrange
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

        // Act
        using var doc = await WriteAndReadReportAsync(results);
        var run = doc.RootElement.GetProperty("runs")[0];

        // Assert
        run.GetProperty("tool").GetProperty("driver").GetProperty("rules").GetArrayLength().Should().Be(1);
        run.GetProperty("results").GetArrayLength().Should().Be(2);
    }

    #region Severity mapping

    [Test]
    public async Task WriteReportAsync_BlockerSeverity_MapsToError()
    {
        // Arrange
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "test.slnx",
                HasErrors = true,
                Errors = [new ValidationError(ValidationErrorCode.ReferencedFileNotFound, "File not found")]
            }
        };
        var overrides = new Dictionary<ValidationErrorCode, RuleSeverity?>
        {
            [ValidationErrorCode.ReferencedFileNotFound] = RuleSeverity.BLOCKER
        };

        // Act
        using var doc = await WriteAndReadReportAsync(results, overrides);

        // Assert
        doc.RootElement.GetProperty("runs")[0].GetProperty("results")[0]
            .GetProperty("level").GetString().Should().Be("error");
    }

    [Test]
    public async Task WriteReportAsync_MinorSeverity_MapsToWarning()
    {
        // Arrange
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "test.slnx",
                HasErrors = true,
                Errors = [new ValidationError(ValidationErrorCode.ReferencedFileNotFound, "File not found")]
            }
        };
        var overrides = new Dictionary<ValidationErrorCode, RuleSeverity?>
        {
            [ValidationErrorCode.ReferencedFileNotFound] = RuleSeverity.MINOR
        };

        // Act
        using var doc = await WriteAndReadReportAsync(results, overrides);

        // Assert
        doc.RootElement.GetProperty("runs")[0].GetProperty("results")[0]
            .GetProperty("level").GetString().Should().Be("warning");
        doc.RootElement.GetProperty("runs")[0]
            .GetProperty("tool").GetProperty("driver").GetProperty("rules")[0]
            .GetProperty("defaultConfiguration").GetProperty("level").GetString().Should().Be("warning");
    }

    [Test]
    public async Task WriteReportAsync_InfoSeverity_MapsToNote()
    {
        // Arrange
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "test.slnx",
                HasErrors = true,
                Errors = [new ValidationError(ValidationErrorCode.ReferencedFileNotFound, "File not found")]
            }
        };
        var overrides = new Dictionary<ValidationErrorCode, RuleSeverity?>
        {
            [ValidationErrorCode.ReferencedFileNotFound] = RuleSeverity.INFO
        };

        // Act
        using var doc = await WriteAndReadReportAsync(results, overrides);

        // Assert
        doc.RootElement.GetProperty("runs")[0].GetProperty("results")[0]
            .GetProperty("level").GetString().Should().Be("note");
    }

    #endregion

    #region Severity overrides

    [Test]
    public async Task WriteReportAsync_IgnoredCode_NotInRulesOrResults()
    {
        // Arrange
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "test.slnx",
                HasErrors = true,
                Errors = [new ValidationError(ValidationErrorCode.ReferencedFileNotFound, "File not found")]
            }
        };
        var overrides = new Dictionary<ValidationErrorCode, RuleSeverity?>
        {
            [ValidationErrorCode.ReferencedFileNotFound] = null
        };

        // Act
        using var doc = await WriteAndReadReportAsync(results, overrides);
        var run = doc.RootElement.GetProperty("runs")[0];

        // Assert
        run.GetProperty("tool").GetProperty("driver").GetProperty("rules").GetArrayLength().Should().Be(0);
        run.GetProperty("results").GetArrayLength().Should().Be(0);
    }

    [Test]
    public async Task WriteReportAsync_IgnoreAllButOne_OnlyVisibleCodeInReport()
    {
        // Arrange
        var results = new List<FileValidationResult>
        {
            new()
            {
                File = "test.slnx",
                HasErrors = true,
                Errors =
                [
                    new ValidationError(ValidationErrorCode.ReferencedFileNotFound, "File not found"),
                    new ValidationError(ValidationErrorCode.XsdViolation, "Schema error", Line: 2),
                ]
            }
        };
        // Ignore all codes, but make SLNX013 (XsdViolation) MAJOR
        var overrides = Enum.GetValues<ValidationErrorCode>()
            .ToDictionary(c => c, _ => (RuleSeverity?)null);
        overrides[ValidationErrorCode.XsdViolation] = RuleSeverity.MAJOR;

        // Act
        using var doc = await WriteAndReadReportAsync(results, overrides);
        var run = doc.RootElement.GetProperty("runs")[0];

        // Assert
        run.GetProperty("tool").GetProperty("driver").GetProperty("rules").GetArrayLength().Should().Be(1);
        run.GetProperty("tool").GetProperty("driver").GetProperty("rules")[0].GetProperty("id").GetString().Should().Be("SLNX013");
        run.GetProperty("results").GetArrayLength().Should().Be(1);
        run.GetProperty("results")[0].GetProperty("ruleId").GetString().Should().Be("SLNX013");
    }

    #endregion

    #region File output

    [Test]
    public async Task WriteReportAsync_WithOutputPath_CreatesParentDirectory()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var reporter = new SarifReporter(fileSystem);
        var results = new List<FileValidationResult>
        {
            new() { File = "test.slnx", HasErrors = false, Errors = [] }
        };

        // Act
        await reporter.WriteReportAsync(results, "output/reports/results.sarif");

        // Assert
        fileSystem.CreatedDirectories.Should().Equal([Path.Combine("output", "reports")]);
    }

    [Test]
    public async Task WriteReportAsync_WithOutputPathNoSubdirectory_DoesNotCreateDirectory()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var reporter = new SarifReporter(fileSystem);
        var results = new List<FileValidationResult>
        {
            new() { File = "test.slnx", HasErrors = false, Errors = [] }
        };

        // Act
        await reporter.WriteReportAsync(results, "results.sarif");

        // Assert
        fileSystem.CreatedDirectories.Should().BeEmpty();
    }

    #endregion

    #region Snapshot tests

    [Test]
    public async Task WriteReportAsync_MatchesSnapshot()
    {
        // Arrange
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

        // Act
        using var stream = new MemoryStream();
        await CreateReporter().WriteReportAsync(results, stream);

        // Assert
        await Verify(stream, "json");
    }

    [Test]
    public async Task WriteReportAsync_AllErrorCodes_MatchesSnapshot()
    {
        // Arrange
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

        // Act
        using var stream = new MemoryStream();
        await CreateReporter().WriteReportAsync(results, stream);

        // Assert
        await Verify(stream, "json");
    }

    #endregion
}
