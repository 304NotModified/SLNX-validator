namespace JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;

internal sealed record SonarRule
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string EngineId { get; init; }
    public required SonarCleanCodeAttribute CleanCodeAttribute { get; init; }
    public required SonarRuleType Type { get; init; }
    public required SonarRuleSeverity Severity { get; init; }
    public required List<SonarImpact> Impacts { get; init; }
}