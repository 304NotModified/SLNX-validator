namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

internal sealed record SarifResult
{
    public required string RuleId { get; init; }
    public required string Level { get; init; }
    public required SarifMessage Message { get; init; }
    public required List<SarifLocation> Locations { get; init; }
}
