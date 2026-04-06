namespace JulianVerdurmen.SlnxValidator.Core.Reporting;

public sealed record ResolvedRule(
    string Id,
    string Name,
    string Description,
    RuleSeverity EffectiveSeverity);
