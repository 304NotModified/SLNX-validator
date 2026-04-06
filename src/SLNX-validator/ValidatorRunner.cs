using JulianVerdurmen.SlnxValidator.Core;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.Reporting;
using JulianVerdurmen.SlnxValidator.Core.SarifReporting;
using JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator;

internal sealed class ValidatorRunner(SlnxCollector collector, ISonarReporter sonarReporter, ISarifReporter sarifReporter, IRequiredFilesChecker requiredFilesChecker, IFileSystem fileSystem)
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
            await Console.Error.WriteLineAsync($"No .slnx files found for input: {options.Input}");
            return options.ContinueOnError ? 0 : 1;
        }

        var overrides = options.SeverityOverrides;
        await ValidationReporter.Report(results, overrides);

        if (options.SonarqubeReportPath is not null)
        {
            await sonarReporter.WriteReportAsync(results, options.SonarqubeReportPath, overrides);
            var size = fileSystem.GetFileSize(options.SonarqubeReportPath);
            Console.WriteLine($"SonarQube report written to: {options.SonarqubeReportPath} ({size} bytes)");
        }

        if (options.SarifReportPath is not null)
        {
            await sarifReporter.WriteReportAsync(results, options.SarifReportPath, overrides);
            var size = fileSystem.GetFileSize(options.SarifReportPath);
            Console.WriteLine($"SARIF report written to: {options.SarifReportPath} ({size} bytes)");
        }

        var hasErrors = results.Any(r => r.Errors.Any(e => IsFailingError(e.Code, overrides)));
        return !options.ContinueOnError && hasErrors ? 1 : 0;
    }

    private static bool IsFailingError(ValidationErrorCode code,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? overrides)
    {
        if (overrides is not null && overrides.TryGetValue(code, out var severity))
            return severity is RuleSeverity.BLOCKER or RuleSeverity.CRITICAL or RuleSeverity.MAJOR;
        return true; // default: all errors are failing
    }
}
