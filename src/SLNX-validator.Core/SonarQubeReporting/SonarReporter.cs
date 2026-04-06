using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.Reporting;
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

    public async Task WriteReportAsync(IReadOnlyList<FileValidationResult> results, string outputPath,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? severityOverrides = null)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            fileSystem.CreateDirectory(directory);

        await using var stream = fileSystem.CreateFile(outputPath);
        await WriteReportAsync(results, stream, severityOverrides);
    }

    public async Task WriteReportAsync(IReadOnlyList<FileValidationResult> results, Stream outputStream,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? severityOverrides = null)
    {
        var usedCodes = results
            .SelectMany(r => r.Errors)
            .Select(e => e.Code)
            .Where(c => !IsIgnored(c, severityOverrides))
            .Distinct()
            .OrderBy(c => (int)c)
            .ToList();

        var rules = usedCodes.Select(c => ApplyOverride(GetRuleDefinition(c), c, severityOverrides)).ToList();

        var issues = results
            .SelectMany(r => r.Errors
                .Where(e => !IsIgnored(e.Code, severityOverrides))
                .Select(e => BuildIssue(r.File, e)))
            .ToList();

        var report = new SonarReport { Rules = rules, Issues = issues };

        await JsonSerializer.SerializeAsync(outputStream, report, JsonOptions);
    }

    private static SonarIssue BuildIssue(string filePath, ValidationError error) => new()
    {
        RuleId = error.Code.ToCode(),
        PrimaryLocation = new SonarLocation
        {
            Message = error.ShortMessage ?? error.Message,
            FilePath = filePath,
            TextRange = error.Line.HasValue ? new SonarTextRange { StartLine = error.Line.Value } : null
        }
    };

    private static bool IsIgnored(ValidationErrorCode code, IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? overrides) =>
        overrides is not null && overrides.TryGetValue(code, out var severity) && severity is null;

    private static SonarRule ApplyOverride(SonarRule rule, ValidationErrorCode code,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? overrides)
    {
        if (overrides is not null && overrides.TryGetValue(code, out var severity) && severity.HasValue)
            return rule with { Severity = severity.Value };
        return rule;
    }

    private static SonarRule GetRuleDefinition(ValidationErrorCode code)
    {
        var meta = RuleMetadataProvider.Get(code);
        return CreateRule(code, meta.Name, meta.Description, GetSonarRuleType(code),
            meta.DefaultSeverity, GetCleanCodeAttribute(code), GetImpactSeverity(code));
    }

    private static SonarRuleType GetSonarRuleType(ValidationErrorCode code) => SonarRuleType.BUG;

    private static SonarCleanCodeAttribute GetCleanCodeAttribute(ValidationErrorCode code) => code switch
    {
        ValidationErrorCode.InvalidExtension => SonarCleanCodeAttribute.CONVENTIONAL,
        _ => SonarCleanCodeAttribute.COMPLETE
    };

    private static SonarImpactSeverity GetImpactSeverity(ValidationErrorCode code) => SonarImpactSeverity.MEDIUM;

    private static SonarRule CreateRule(ValidationErrorCode code, string name, string description,
        SonarRuleType type, RuleSeverity severity, SonarCleanCodeAttribute cleanCodeAttribute, SonarImpactSeverity impactSeverity) => new()
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