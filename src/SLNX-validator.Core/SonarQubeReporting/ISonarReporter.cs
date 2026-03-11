using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;

public interface ISonarReporter
{
    Task WriteReportAsync(IReadOnlyList<FileValidationResult> results, string outputPath);
    Task WriteReportAsync(IReadOnlyList<FileValidationResult> results, Stream outputStream);
}