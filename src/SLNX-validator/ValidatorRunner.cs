using JulianVerdurmen.SlnxValidator.Core;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.Reporting;
using JulianVerdurmen.SlnxValidator.Core.SarifReporting;
using JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;
using JulianVerdurmen.SlnxValidator.Core.Validation;

namespace JulianVerdurmen.SlnxValidator;

internal sealed class ValidatorRunner(SlnxCollector collector, ISonarReporter sonarReporter, ISarifReporter sarifReporter, IRequiredFilesChecker requiredFilesChecker, IFileSystem fileSystem, IConsole console)
{
    public async Task<int> RunAsync(ValidatorRunnerOptions options, CancellationToken cancellationToken)
    {
        // Resolve required file glob patterns to absolute disk paths (once for all .slnx files).
        RequiredFilesOptions? requiredFilesOptions = null;
        if (options.RequiredFilesPattern is not null)
        {
            var matchedPaths = requiredFilesChecker.ResolveMatchedPaths(options.RequiredFilesPattern, options.WorkingDirectory);
            requiredFilesOptions = new RequiredFilesOptions(matchedPaths, options.RequiredFilesPattern);
        }

        var results = await collector.CollectAsync(options.Input, requiredFilesOptions, cancellationToken);

        if (results.Count == 0)
        {
            await console.WriteErrorLineAsync($"No .slnx files found for input: {options.Input}");
            return options.ContinueOnError ? 0 : 1;
        }

        var reportResults = new ReportResults(results, options.SeverityOverrides);
        await ValidationReporter.Report(reportResults);

        if (options.SonarqubeReportPath is not null)
        {
            await sonarReporter.WriteReportAsync(reportResults, options.SonarqubeReportPath);
            var size = fileSystem.GetFileSize(options.SonarqubeReportPath);
            await console.WriteLineAsync($"SonarQube report written to: {options.SonarqubeReportPath} ({size} bytes)");
        }

        if (options.SarifReportPath is not null)
        {
            await sarifReporter.WriteReportAsync(reportResults, options.SarifReportPath);
            var size = fileSystem.GetFileSize(options.SarifReportPath);
            await console.WriteLineAsync($"SARIF report written to: {options.SarifReportPath} ({size} bytes)");
        }

        var hasErrors = results.Any(r => r.Errors.Any(e => options.SeverityOverrides.IsFailingError(e.Code)));
        return !options.ContinueOnError && hasErrors ? 1 : 0;
    }
}
