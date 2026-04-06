namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

internal sealed record SarifMessage
{
    public required string Text { get; init; }
}
