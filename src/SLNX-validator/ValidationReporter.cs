using JulianVerdurmen.SlnxValidator.Core.Reporting;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator;

internal static class ValidationReporter
{
    public static async Task Report(ReportResults reportResults)
    {
        var results = reportResults.Results;
        var overrides = reportResults.Overrides;

        foreach (var result in results)
        {
            var isFailingResult = result.Errors.Any(e => overrides.IsFailingError(e.Code));
            Console.WriteLine(isFailingResult ? $"[FAIL] {result.File}" : $"[OK]   {result.File}");
        }

        var visibleResults = results
            .Where(r => r.Errors.Any(e => overrides.IsVisible(e.Code)))
            .ToList();

        if (visibleResults.Count == 0)
        {
            return;
        }

        Console.WriteLine();

        foreach (var result in visibleResults)
        {
            await Console.Error.WriteLineAsync(result.File);

            foreach (var error in result.Errors.Where(e => overrides.IsVisible(e.Code)))
            {
                await Console.Error.WriteLineAsync(FormatError(error));
            }
        }
    }

    private static string FormatError(ValidationError error)
    {
        var location = error.Line is null ? "" : $"line {error.Line}: ";
        return $"  - {location}[{error.Code.ToCode()}] {error.Message}";
    }
}
