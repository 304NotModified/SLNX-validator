using System.Collections;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Reporting;

public sealed class SeverityOverrides : IEnumerable<KeyValuePair<ValidationErrorCode, RuleSeverity?>>
{
    private readonly IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?> _overrides;

    public static readonly SeverityOverrides Empty = new(new Dictionary<ValidationErrorCode, RuleSeverity?>());

    public int Count => _overrides.Count;

    public RuleSeverity? this[ValidationErrorCode code] => _overrides[code];

    public SeverityOverrides(IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?> overrides)
    {
        _overrides = overrides;
    }

    public bool IsIgnored(ValidationErrorCode code) =>
        _overrides.TryGetValue(code, out var severity) && severity is null;

    public bool IsVisible(ValidationErrorCode code) =>
        !_overrides.TryGetValue(code, out var severity) || severity is not null;

    internal bool TryGetOverride(ValidationErrorCode code, out RuleSeverity severity)
    {
        if (_overrides.TryGetValue(code, out var value) && value.HasValue)
        {
            severity = value.Value;
            return true;
        }

        severity = default;
        return false;
    }

    public IReadOnlyList<ValidationErrorCode> GetUsedCodes(IReadOnlyList<FileValidationResult> results) =>
        results
            .SelectMany(r => r.Errors)
            .Select(e => e.Code)
            .Where(c => !IsIgnored(c))
            .Distinct()
            .OrderBy(c => (int)c)
            .ToList();

    public IEnumerator<KeyValuePair<ValidationErrorCode, RuleSeverity?>> GetEnumerator() =>
        _overrides.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
