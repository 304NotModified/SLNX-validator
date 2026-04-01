using JulianVerdurmen.SlnxValidator.Core;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;
using JulianVerdurmen.SlnxValidator.Core.Validation;

namespace JulianVerdurmen.SlnxValidator;

internal sealed class ValidatorRunner(ISlnxFileResolver resolver, ValidationCollector collector, ISonarReporter sonarReporter, IRequiredFilesChecker requiredFilesChecker, IFileSystem fileSystem)
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

        await ValidationReporter.Report(results);

        if (options.SonarqubeReportPath is not null)
        {
            await sonarReporter.WriteReportAsync(results, options.SonarqubeReportPath);
            var size = fileSystem.GetFileSize(options.SonarqubeReportPath);
            Console.WriteLine($"SonarQube report written to: {options.SonarqubeReportPath} ({size} bytes)");
        }

        var hasErrors = results.Any(r => r.HasErrors);
        return !options.ContinueOnError && hasErrors ? 1 : 0;
    }
}
