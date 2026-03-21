using JulianVerdurmen.SlnxValidator.Core;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;
using JulianVerdurmen.SlnxValidator.Core.Validation;

namespace JulianVerdurmen.SlnxValidator;

internal sealed class ValidatorRunner(ISlnxFileResolver resolver, ValidationCollector collector, ISonarReporter sonarReporter, IRequiredFilesChecker requiredFilesChecker)
{
    public async Task<int> RunAsync(string input, string? sonarqubeReportPath, bool continueOnError,
        string? requiredFilesPattern, string workingDirectory, CancellationToken cancellationToken)
    {
        var files = resolver.Resolve(input);

        if (files.Count == 0)
        {
            await Console.Error.WriteLineAsync($"No .slnx files found for input: {input}");
            return continueOnError ? 0 : 1;
        }

        // Resolve required file glob patterns to absolute disk paths (once for all .slnx files).
        IReadOnlyList<string>? matchedRequiredPaths = null;
        if (requiredFilesPattern is not null)
            matchedRequiredPaths = requiredFilesChecker.ResolveMatchedPaths(requiredFilesPattern, workingDirectory);

        var results = await collector.CollectAsync(files, matchedRequiredPaths, requiredFilesPattern, cancellationToken);

        await ValidationReporter.Report(results);

        if (sonarqubeReportPath is not null)
            await sonarReporter.WriteReportAsync(results, sonarqubeReportPath);

        var hasErrors = results.Any(r => r.HasErrors);
        return !continueOnError && hasErrors ? 1 : 0;
    }
}
