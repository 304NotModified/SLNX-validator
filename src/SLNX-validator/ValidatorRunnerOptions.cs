using JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator;

/// <summary>All options forwarded from the CLI to <see cref="ValidatorRunner.RunAsync"/>.</summary>
internal sealed record ValidatorRunnerOptions(
    string Input,
    string? SonarqubeReportPath,
    bool ContinueOnError,
    string? RequiredFilesPattern,
    string WorkingDirectory,
    IReadOnlyDictionary<ValidationErrorCode, SonarRuleSeverity?>? SeverityOverrides = null);
