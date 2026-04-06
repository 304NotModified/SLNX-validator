namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

internal sealed record SarifTool
{
    public required SarifToolComponent Driver { get; init; }
}
