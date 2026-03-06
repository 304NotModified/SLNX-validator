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
            Console.Error.WriteLine($"No .slnx files found for input: {input}");
            return 1;
        }

        var results = await collector.CollectAsync(files, cancellationToken);

        reporter.Report(results);

        return results.Any(r => r.HasErrors) ? 1 : 0;
    }
}
