using JulianVerdurmen.SlnxValidator.Core;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator;

internal sealed class ValidatorRunner(ISlnxFileResolver resolver, ValidationCollector collector, ISonarReporter sonarReporter, IRequiredFilesChecker requiredFilesChecker)
{
    public async Task<int> RunAsync(ValidatorRunnerOptions options, CancellationToken cancellationToken)
    {
        var files = resolver.Resolve(options.Input);

        if (files.Count == 0)
        {
            await Console.Error.WriteLineAsync($"No .slnx files found for input: {options.Input}");
            return options.ContinueOnError ? 0 : 1;
        }

        // Resolve required file glob patterns to absolute disk paths (once for all .slnx files).
        RequiredFilesOptions? requiredFilesOptions = null;
        if (options.RequiredFilesPattern is not null)
        {
            var matchedPaths = requiredFilesChecker.ResolveMatchedPaths(options.RequiredFilesPattern, options.WorkingDirectory);
            requiredFilesOptions = new RequiredFilesOptions(matchedPaths, options.RequiredFilesPattern);
        }

        var results = await collector.CollectAsync(files, requiredFilesOptions, cancellationToken);

        var overrides = options.SeverityOverrides;
        await ValidationReporter.Report(results, overrides);

        if (options.SonarqubeReportPath is not null)
            await sonarReporter.WriteReportAsync(results, options.SonarqubeReportPath, overrides);

        var hasErrors = results.Any(r => r.Errors.Any(e => IsFailingError(e.Code, overrides)));
        return !options.ContinueOnError && hasErrors ? 1 : 0;
    }

    private static bool IsFailingError(ValidationErrorCode code,
        IReadOnlyDictionary<ValidationErrorCode, SonarRuleSeverity?>? overrides)
    {
        if (overrides is not null && overrides.TryGetValue(code, out var severity))
            return severity is SonarRuleSeverity.BLOCKER or SonarRuleSeverity.CRITICAL or SonarRuleSeverity.MAJOR;
        return true; // default: all errors are failing
    }
}
