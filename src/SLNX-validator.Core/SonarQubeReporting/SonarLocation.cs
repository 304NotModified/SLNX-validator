namespace JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;

internal sealed record SonarLocation
{
    public required string Message { get; init; }
    public required string FilePath { get; init; }
    public SonarTextRange? TextRange { get; init; }
}