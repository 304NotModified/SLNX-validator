namespace JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;

internal sealed record SonarReport
{
    public required List<SonarRule> Rules { get; init; }
    public required List<SonarIssue> Issues { get; init; }
}