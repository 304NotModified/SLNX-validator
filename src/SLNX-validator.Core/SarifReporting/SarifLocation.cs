namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

internal sealed record SarifLocation
{
    public required SarifPhysicalLocation PhysicalLocation { get; init; }
}
