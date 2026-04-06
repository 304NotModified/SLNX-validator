namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

internal sealed record SarifRegion
{
    public required int StartLine { get; init; }
}
