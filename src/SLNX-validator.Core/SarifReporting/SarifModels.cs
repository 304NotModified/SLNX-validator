using System.Text.Json.Serialization;

namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

internal sealed record SarifLog
{
    [JsonPropertyName("$schema")]
    public required string Schema { get; init; }

    public required string Version { get; init; }

    public required List<SarifRun> Runs { get; init; }
}

internal sealed record SarifRun
{
    public required SarifTool Tool { get; init; }
    public required List<SarifResult> Results { get; init; }
}

internal sealed record SarifTool
{
    public required SarifToolComponent Driver { get; init; }
}

internal sealed record SarifToolComponent
{
    public required string Name { get; init; }
    public required string InformationUri { get; init; }
    public required List<SarifReportingDescriptor> Rules { get; init; }
}

internal sealed record SarifReportingDescriptor
{
    public required string Id { get; init; }
    public required SarifMessage ShortDescription { get; init; }
    public required SarifMessage FullDescription { get; init; }
    public required SarifDefaultConfiguration DefaultConfiguration { get; init; }
}

internal sealed record SarifDefaultConfiguration
{
    public required string Level { get; init; }
}

internal sealed record SarifResult
{
    public required string RuleId { get; init; }
    public required string Level { get; init; }
    public required SarifMessage Message { get; init; }
    public required List<SarifLocation> Locations { get; init; }
}

internal sealed record SarifLocation
{
    public required SarifPhysicalLocation PhysicalLocation { get; init; }
}

internal sealed record SarifPhysicalLocation
{
    public required SarifArtifactLocation ArtifactLocation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SarifRegion? Region { get; init; }
}

internal sealed record SarifArtifactLocation
{
    public required string Uri { get; init; }
}

internal sealed record SarifRegion
{
    public required int StartLine { get; init; }
}

internal sealed record SarifMessage
{
    public required string Text { get; init; }
}
