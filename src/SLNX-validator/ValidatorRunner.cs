using JulianVerdurmen.SlnxValidator.Core;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;

namespace JulianVerdurmen.SlnxValidator;

internal sealed class ValidatorRunner(SlnxFileResolver resolver, ValidationCollector collector, ValidationReporter reporter)
{
    public async Task<int> RunAsync(string input, CancellationToken cancellationToken)
    {
        var files = resolver.Resolve(input);

        if (files.Count == 0)
        {
            await Console.Error.WriteLineAsync($"No .slnx files found for input: {input}");
            return 1;
        }

        var results = await collector.CollectAsync(files, cancellationToken);

        await reporter.Report(results);

        return results.Any(r => r.HasErrors) ? 1 : 0;
    }
}
