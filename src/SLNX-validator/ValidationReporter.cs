using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator;

internal class ValidationReporter
{
    public async Task Report(IReadOnlyList<FileValidationResult> results)
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
            await Console.Error.WriteLineAsync(result.File);

            foreach (var error in result.Errors)
            {
                await Console.Error.WriteLineAsync(FormatError(error));
            }
        }
    }

    private string FormatError(ValidationError error)
    {
        var location = error.Line is null ? "" : $"line {error.Line}: ";
        return $"  - {location}[{error.Code.ToCode()}] {error.Message}";
    }
}
