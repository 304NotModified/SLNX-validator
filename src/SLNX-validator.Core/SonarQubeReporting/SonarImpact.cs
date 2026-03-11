namespace JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;

internal sealed record SonarImpact
{
    public required SonarSoftwareQuality SoftwareQuality { get; init; }
    public required SonarImpactSeverity Severity { get; init; }
}