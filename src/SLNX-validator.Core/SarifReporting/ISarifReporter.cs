using JulianVerdurmen.SlnxValidator.Core.Reporting;

namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

public interface ISarifReporter
{
    Task WriteReportAsync(ReportResults results, string outputPath);

    Task WriteReportAsync(ReportResults results, Stream outputStream);
}
