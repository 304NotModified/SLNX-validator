namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

internal sealed record SarifReportingDescriptor
{
    public required string Id { get; init; }
    public required SarifMessage ShortDescription { get; init; }
    public required SarifMessage FullDescription { get; init; }
    public required SarifDefaultConfiguration DefaultConfiguration { get; init; }
}
