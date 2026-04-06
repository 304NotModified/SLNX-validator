namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

internal sealed record SarifToolComponent
{
    public required string Name { get; init; }
    public required string InformationUri { get; init; }
    public required List<SarifReportingDescriptor> Rules { get; init; }
}
