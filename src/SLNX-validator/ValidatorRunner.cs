using JulianVerdurmen.SlnxValidator.Core;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;

namespace JulianVerdurmen.SlnxValidator;

internal sealed class ValidatorRunner(SlnxFileResolver resolver, ValidationCollector collector)
{
    public async Task<int> RunAsync(string input, string? sonarqubeReportPath, bool continueOnError, CancellationToken cancellationToken)
    {
        var files = resolver.Resolve(input);

        if (files.Count == 0)
        {
            await Console.Error.WriteLineAsync($"No .slnx files found for input: {input}");
            return continueOnError ? 0 : 1;
        }

        var results = await collector.CollectAsync(files, cancellationToken);

        await ValidationReporter.Report(results);

        if (sonarqubeReportPath is not null)
            await SonarReporter.WriteReportAsync(results, sonarqubeReportPath);

        var hasErrors = results.Any(r => r.HasErrors);
        return !continueOnError && hasErrors ? 1 : 0;
    }
}
