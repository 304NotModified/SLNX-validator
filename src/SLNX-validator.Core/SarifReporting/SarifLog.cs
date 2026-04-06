using System.Text.Json.Serialization;

namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

internal sealed record SarifLog
{
    [JsonPropertyName("$schema")]
    public required string Schema { get; init; }

    public required string Version { get; init; }

    public required List<SarifRun> Runs { get; init; }
}
