namespace JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;

internal sealed record SonarIssue
{
    public required string RuleId { get; init; }
    public required SonarLocation PrimaryLocation { get; init; }
}