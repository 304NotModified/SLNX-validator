using JulianVerdurmen.SlnxValidator.Core.Reporting;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator;

internal static class ValidationReporter
{
    public static async Task Report(IReadOnlyList<FileValidationResult> results,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? severityOverrides = null)
    {
        foreach (var result in results)
        {
            var isFailingResult = result.Errors.Any(e => IsFailingError(e.Code, severityOverrides));
            Console.WriteLine(isFailingResult ? $"[FAIL] {result.File}" : $"[OK]   {result.File}");
        }

        var visibleResults = results
            .Where(r => r.Errors.Any(e => IsVisible(e.Code, severityOverrides)))
            .ToList();

        if (visibleResults.Count == 0)
        {
            return;
        }

        Console.WriteLine();

        foreach (var result in visibleResults)
        {
            await Console.Error.WriteLineAsync(result.File);

            foreach (var error in result.Errors.Where(e => IsVisible(e.Code, severityOverrides)))
            {
                await Console.Error.WriteLineAsync(FormatError(error));
            }
        }
    }

    private static bool IsVisible(ValidationErrorCode code,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? overrides) =>
        overrides is null || !overrides.TryGetValue(code, out var severity) || severity is not null;

    private static bool IsFailingError(ValidationErrorCode code,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? overrides)
    {
        if (overrides is not null && overrides.TryGetValue(code, out var severity))
            return severity is RuleSeverity.BLOCKER or RuleSeverity.CRITICAL or RuleSeverity.MAJOR;
        return true; // default: all errors are failing
    }

    private static string FormatError(ValidationError error)
    {
        var location = error.Line is null ? "" : $"line {error.Line}: ";
        return $"  - {location}[{error.Code.ToCode()}] {error.Message}";
    }
}
