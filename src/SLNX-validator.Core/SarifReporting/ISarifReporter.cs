using JulianVerdurmen.SlnxValidator.Core.Reporting;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

public interface ISarifReporter
{
    Task WriteReportAsync(IReadOnlyList<FileValidationResult> results, string outputPath,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? severityOverrides = null);

    Task WriteReportAsync(IReadOnlyList<FileValidationResult> results, Stream outputStream,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? severityOverrides = null);
}
