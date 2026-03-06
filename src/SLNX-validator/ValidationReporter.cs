using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator;

internal sealed class ValidationReporter
{
    public void Report(IReadOnlyList<FileValidationResult> results)
    {
        foreach (var result in results)
        {
            Console.WriteLine(result.HasErrors ? $"[FAIL] {result.File}" : $"[OK]   {result.File}");
        }

        var failedResults = results.Where(r => r.HasErrors).ToList();

        if (failedResults.Count == 0)
        {
            return;
        }

        Console.WriteLine();

        foreach (var result in failedResults)
        {
            Console.Error.WriteLine(result.File);

            foreach (var error in result.Errors)
            {
                Console.Error.WriteLine(FormatError(error));
            }
        }
    }

    private static string FormatError(ValidationError error)
    {
        var location = error.Line is null ? "" : $"line {error.Line}: ";
        return $"  - {location}[{error.Code.ToCode()}] {error.Message}";
    }
}
