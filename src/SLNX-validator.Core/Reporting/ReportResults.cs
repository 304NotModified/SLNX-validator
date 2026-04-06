using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Reporting;

public sealed class ReportResults
{
    public IReadOnlyList<FileValidationResult> Results { get; }
    public SeverityOverrides Overrides { get; }

    public ReportResults(IReadOnlyList<FileValidationResult> results, SeverityOverrides? overrides = null)
    {
        Results = results;
        Overrides = overrides ?? SeverityOverrides.Empty;
    }

    public IReadOnlyList<ValidationErrorCode> UsedCodes => Overrides.GetUsedCodes(Results);
}
