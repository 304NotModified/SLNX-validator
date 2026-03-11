using System.Text.Json;
using System.Text.Json.Serialization;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator;

internal static class SonarReporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task WriteReportAsync(IReadOnlyList<FileValidationResult> results, string outputPath)
    {
        var usedCodes = results
            .SelectMany(r => r.Errors)
            .Select(e => e.Code)
            .Distinct()
            .OrderBy(c => (int)c)
            .ToList();

        var rules = usedCodes.Select(GetRuleDefinition).ToList();

        var issues = results
            .SelectMany(r => r.Errors.Select(e => BuildIssue(r.File, e)))
            .ToList();

        var report = new SonarReport { Rules = rules, Issues = issues };

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var stream = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(stream, report, JsonOptions);
    }

    private static SonarIssue BuildIssue(string filePath, ValidationError error) => new()
    {
        RuleId = error.Code.ToCode(),
        PrimaryLocation = new SonarLocation
        {
            Message = error.Message,
            FilePath = filePath,
            TextRange = error.Line.HasValue ? new SonarTextRange { StartLine = error.Line.Value } : null
        }
    };

    private static SonarRule GetRuleDefinition(ValidationErrorCode code) => code switch
    {
        ValidationErrorCode.FileNotFound => CreateRule(code,
            "Input file not found",
            "The specified .slnx file does not exist.",
            "BUG", "MAJOR", "COMPLETE", "HIGH"),

        ValidationErrorCode.InvalidExtension => CreateRule(code,
            "Invalid file extension",
            "The input file does not have a .slnx extension.",
            "CODE_SMELL", "MINOR", "CONVENTIONAL", "MEDIUM"),

        ValidationErrorCode.NotATextFile => CreateRule(code,
            "File is not a text file",
            "The file is binary and cannot be parsed as XML.",
            "BUG", "MAJOR", "COMPLETE", "HIGH"),

        ValidationErrorCode.InvalidXml => CreateRule(code,
            "Invalid XML",
            "The .slnx file is not valid XML.",
            "BUG", "MAJOR", "CONVENTIONAL", "HIGH"),

        ValidationErrorCode.ReferencedFileNotFound => CreateRule(code,
            "Referenced file not found",
            "A file referenced in a <File Path=\"...\"> element does not exist on disk.",
            "BUG", "MAJOR", "COMPLETE", "HIGH"),

        ValidationErrorCode.InvalidWildcardUsage => CreateRule(code,
            "Invalid wildcard usage",
            "A <File Path=\"...\"> element contains a wildcard pattern, which is not supported.",
            "CODE_SMELL", "MINOR", "CONVENTIONAL", "MEDIUM"),

        ValidationErrorCode.XsdViolation => CreateRule(code,
            "XSD schema violation",
            "The XML structure violates the .slnx schema.",
            "BUG", "MAJOR", "CONVENTIONAL", "HIGH"),

        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };

    private static SonarRule CreateRule(ValidationErrorCode code, string name, string description,
        string type, string severity, string cleanCodeAttribute, string impactSeverity) => new()
    {
        Id = code.ToCode(),
        Name = name,
        Description = description,
        EngineId = "slnx-validator",
        CleanCodeAttribute = cleanCodeAttribute,
        Type = type,
        Severity = severity,
        Impacts = [new SonarImpact { SoftwareQuality = "MAINTAINABILITY", Severity = impactSeverity }]
    };

    private sealed record SonarReport
    {
        public required List<SonarRule> Rules { get; init; }
        public required List<SonarIssue> Issues { get; init; }
    }

    private sealed record SonarRule
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required string EngineId { get; init; }
        public required string CleanCodeAttribute { get; init; }
        public required string Type { get; init; }
        public required string Severity { get; init; }
        public required List<SonarImpact> Impacts { get; init; }
    }

    private sealed record SonarImpact
    {
        public required string SoftwareQuality { get; init; }
        public required string Severity { get; init; }
    }

    private sealed record SonarIssue
    {
        public required string RuleId { get; init; }
        public required SonarLocation PrimaryLocation { get; init; }
    }

    private sealed record SonarLocation
    {
        public required string Message { get; init; }
        public required string FilePath { get; init; }
        public SonarTextRange? TextRange { get; init; }
    }

    private sealed record SonarTextRange
    {
        public required int StartLine { get; init; }
    }
}
