using JulianVerdurmen.SlnxValidator.Core.Reporting;

namespace JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;

public interface ISonarReporter
{
    Task WriteReportAsync(ReportResults results, string outputPath);

    Task WriteReportAsync(ReportResults results, Stream outputStream);
}
