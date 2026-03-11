using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;

public sealed class SonarReporter(IFileSystem fileSystem) : ISonarReporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task WriteReportAsync(IReadOnlyList<FileValidationResult> results, string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            fileSystem.CreateDirectory(directory);

        await using var stream = fileSystem.CreateFile(outputPath);
        await WriteReportAsync(results, stream);
    }

    public async Task WriteReportAsync(IReadOnlyList<FileValidationResult> results, Stream outputStream)
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

        await JsonSerializer.SerializeAsync(outputStream, report, JsonOptions);
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
            SonarRuleType.BUG, SonarRuleSeverity.MAJOR, SonarCleanCodeAttribute.COMPLETE, SonarImpactSeverity.HIGH),

        ValidationErrorCode.InvalidExtension => CreateRule(code,
            "Invalid file extension",
            "The input file does not have a .slnx extension.",
            SonarRuleType.BUG, SonarRuleSeverity.MINOR, SonarCleanCodeAttribute.CONVENTIONAL, SonarImpactSeverity.HIGH),

        ValidationErrorCode.NotATextFile => CreateRule(code,
            "File is not a text file",
            "The file is binary and cannot be parsed as XML.",
            SonarRuleType.BUG, SonarRuleSeverity.MAJOR, SonarCleanCodeAttribute.COMPLETE, SonarImpactSeverity.HIGH),

        ValidationErrorCode.InvalidXml => CreateRule(code,
            "Invalid XML",
            "The .slnx file is not valid XML.",
            SonarRuleType.BUG, SonarRuleSeverity.MAJOR, SonarCleanCodeAttribute.COMPLETE, SonarImpactSeverity.HIGH),

        ValidationErrorCode.ReferencedFileNotFound => CreateRule(code,
            "Referenced file not found",
            "A file referenced in a <File Path=\"...\"> element does not exist on disk.",
            SonarRuleType.BUG, SonarRuleSeverity.MAJOR, SonarCleanCodeAttribute.COMPLETE, SonarImpactSeverity.HIGH),

        ValidationErrorCode.InvalidWildcardUsage => CreateRule(code,
            "Invalid wildcard usage",
            "A <File Path=\"...\"> element contains a wildcard pattern, which is not supported.",
            SonarRuleType.BUG, SonarRuleSeverity.MINOR, SonarCleanCodeAttribute.COMPLETE, SonarImpactSeverity.HIGH),

        ValidationErrorCode.XsdViolation => CreateRule(code,
            "XSD schema violation",
            "The XML structure violates the .slnx schema.",
            SonarRuleType.BUG, SonarRuleSeverity.MAJOR, SonarCleanCodeAttribute.COMPLETE, SonarImpactSeverity.MEDIUM),

        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };

    private static SonarRule CreateRule(ValidationErrorCode code, string name, string description,
        SonarRuleType type, SonarRuleSeverity severity, SonarCleanCodeAttribute cleanCodeAttribute, SonarImpactSeverity impactSeverity) => new()
    {
        Id = code.ToCode(),
        Name = name,
        Description = description,
        EngineId = "slnx-validator",
        CleanCodeAttribute = cleanCodeAttribute,
        Type = type,
        Severity = severity,
        Impacts = [new SonarImpact { SoftwareQuality = SonarSoftwareQuality.MAINTAINABILITY, Severity = impactSeverity }]
    };
}