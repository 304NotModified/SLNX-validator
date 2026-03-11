namespace JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;

internal sealed record SonarTextRange
{
    public required int StartLine { get; init; }
}