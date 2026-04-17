using System.Text.Json;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.Reporting;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;

public sealed class SonarReporter(IFileSystem fileSystem) : ReporterBase(fileSystem), ISonarReporter
{
    public override async Task WriteReportAsync(ReportResults results, Stream outputStream)
    {
        var usedCodes = results.UsedCodes;

        var rules = usedCodes.Select(c => BuildRule(c, results.Overrides)).ToList();

        var issues = results.Results
            .SelectMany(r => r.Errors
                .Where(e => !results.Overrides.IsIgnored(e.Code))
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

    private static SonarRule BuildRule(ValidationErrorCode code, SeverityOverrides overrides)
    {
        var resolved = RuleProvider.Resolve(code, overrides);
        return CreateRule(code, resolved.Name, resolved.Description, GetSonarRuleType(),
            resolved.EffectiveSeverity, GetCleanCodeAttribute(code), GetImpactSeverity());
    }

    private static SonarRuleType GetSonarRuleType() => SonarRuleType.BUG;

    private static SonarCleanCodeAttribute GetCleanCodeAttribute(ValidationErrorCode code) => code switch
    {
        ValidationErrorCode.InvalidExtension => SonarCleanCodeAttribute.CONVENTIONAL,
        _ => SonarCleanCodeAttribute.COMPLETE
    };

    private static SonarImpactSeverity GetImpactSeverity() => SonarImpactSeverity.MEDIUM;

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
