using System.Text.Json.Serialization;

namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

internal sealed record SarifPhysicalLocation
{
    public required SarifArtifactLocation ArtifactLocation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SarifRegion? Region { get; init; }
}
