namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

internal sealed record SarifRun
{
    public required SarifTool Tool { get; init; }
    public required List<SarifResult> Results { get; init; }
}
