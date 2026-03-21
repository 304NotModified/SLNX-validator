using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;

public interface ISonarReporter
{
    Task WriteReportAsync(IReadOnlyList<FileValidationResult> results, string outputPath,
        IReadOnlyDictionary<ValidationErrorCode, SonarRuleSeverity?>? severityOverrides = null);

    Task WriteReportAsync(IReadOnlyList<FileValidationResult> results, Stream outputStream,
        IReadOnlyDictionary<ValidationErrorCode, SonarRuleSeverity?>? severityOverrides = null);
}