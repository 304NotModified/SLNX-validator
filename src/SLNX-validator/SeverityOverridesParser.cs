using JulianVerdurmen.SlnxValidator.Core.Reporting;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator;

internal static class SeverityOverridesParser
{
    public static SeverityOverrides Parse(
        string? blocker, string? critical, string? major, string? minor, string? info, string? ignore)
    {
        var result = new Dictionary<ValidationErrorCode, RuleSeverity?>();

        // Pass 1: wildcards only (lowest priority — expanded first so specific codes can overwrite)
        ParseInto(blocker,  RuleSeverity.BLOCKER,  result, wildcardOnly: true);
        ParseInto(critical, RuleSeverity.CRITICAL, result, wildcardOnly: true);
        ParseInto(major,    RuleSeverity.MAJOR,    result, wildcardOnly: true);
        ParseInto(minor,    RuleSeverity.MINOR,    result, wildcardOnly: true);
        ParseInto(info,     RuleSeverity.INFO,     result, wildcardOnly: true);
        ParseInto(ignore,   null,                  result, wildcardOnly: true);

        // Pass 2: specific codes (highest priority — overwrite wildcards from pass 1)
        ParseInto(blocker,  RuleSeverity.BLOCKER,  result, wildcardOnly: false);
        ParseInto(critical, RuleSeverity.CRITICAL, result, wildcardOnly: false);
        ParseInto(major,    RuleSeverity.MAJOR,    result, wildcardOnly: false);
        ParseInto(minor,    RuleSeverity.MINOR,    result, wildcardOnly: false);
        ParseInto(info,     RuleSeverity.INFO,     result, wildcardOnly: false);
        ParseInto(ignore,   null,                  result, wildcardOnly: false);

        return new SeverityOverrides(result);
    }

    private static void ParseInto(string? input, RuleSeverity? severity,
        Dictionary<ValidationErrorCode, RuleSeverity?> target, bool wildcardOnly)
    {
        if (input is null) return;

        if (input.Trim() == "*")
        {
            if (wildcardOnly)
            {
                foreach (var code in Enum.GetValues<ValidationErrorCode>())
                    target[code] = severity;
            }
            return;
        }

        if (wildcardOnly) return;

        foreach (var raw in input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (raw == "*") continue;
            target[ParseCode(raw)] = severity;
        }
    }

    private static ValidationErrorCode ParseCode(string raw)
    {
        if (raw.StartsWith("SLNX", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(raw.AsSpan(4), out var num) &&
            Enum.IsDefined(typeof(ValidationErrorCode), num))
        {
            return (ValidationErrorCode)num;
        }

        if (Enum.TryParse<ValidationErrorCode>(raw, ignoreCase: true, out var code))
            return code;

        throw new InvalidOperationException($"Unknown validation code: '{raw}'. Use the SLNX-prefixed code (e.g. SLNX011) or the enum name (e.g. ReferencedFileNotFound).");
    }
}
