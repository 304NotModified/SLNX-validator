namespace JulianVerdurmen.SlnxValidator.Core.Reporting;

public sealed record RuleMetadata(
    string Id,
    string Name,
    string Description,
    RuleSeverity DefaultSeverity);
