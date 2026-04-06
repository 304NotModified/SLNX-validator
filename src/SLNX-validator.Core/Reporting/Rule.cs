namespace JulianVerdurmen.SlnxValidator.Core.Reporting;

public sealed record Rule(
    string Id,
    string Name,
    string Description,
    RuleSeverity DefaultSeverity);
