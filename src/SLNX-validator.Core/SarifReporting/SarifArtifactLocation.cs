namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

internal sealed record SarifArtifactLocation
{
    public required string Uri { get; init; }
}
