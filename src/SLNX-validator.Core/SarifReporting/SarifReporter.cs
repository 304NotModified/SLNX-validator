using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.Reporting;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

public sealed class SarifReporter(IFileSystem fileSystem) : ISarifReporter
{
    private const string SarifSchema = "https://json.schemastore.org/sarif-2.1.0.json";
    private const string SarifVersion = "2.1.0";
    private const string ToolName = "slnx-validator";
    private const string ToolInformationUri = "https://github.com/304NotModified/SLNX-validator";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task WriteReportAsync(IReadOnlyList<FileValidationResult> results, string outputPath,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? severityOverrides = null)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            fileSystem.CreateDirectory(directory);

        await using var stream = fileSystem.CreateFile(outputPath);
        await WriteReportAsync(results, stream, severityOverrides);
    }

    public async Task WriteReportAsync(IReadOnlyList<FileValidationResult> results, Stream outputStream,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? severityOverrides = null)
    {
        var usedCodes = results
            .SelectMany(r => r.Errors)
            .Select(e => e.Code)
            .Where(c => !IsIgnored(c, severityOverrides))
            .Distinct()
            .OrderBy(c => (int)c)
            .ToList();

        var rules = usedCodes
            .Select(c => BuildRule(c, severityOverrides))
            .ToList();

        var sarifResults = results
            .SelectMany(r => r.Errors
                .Where(e => !IsIgnored(e.Code, severityOverrides))
                .Select(e => BuildResult(r.File, e, severityOverrides)))
            .ToList();

        var log = new SarifLog
        {
            Schema = SarifSchema,
            Version = SarifVersion,
            Runs =
            [
                new SarifRun
                {
                    Tool = new SarifTool
                    {
                        Driver = new SarifToolComponent
                        {
                            Name = ToolName,
                            InformationUri = ToolInformationUri,
                            Rules = rules
                        }
                    },
                    Results = sarifResults
                }
            ]
        };

        await JsonSerializer.SerializeAsync(outputStream, log, JsonOptions);
    }

    private static bool IsIgnored(ValidationErrorCode code, IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? overrides) =>
        overrides is not null && overrides.TryGetValue(code, out var severity) && severity is null;

    private static RuleSeverity GetEffectiveSeverity(ValidationErrorCode code,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? overrides)
    {
        if (overrides is not null && overrides.TryGetValue(code, out var severity) && severity.HasValue)
            return severity.Value;
        return RuleMetadataProvider.Get(code).DefaultSeverity;
    }

    private static SarifReportingDescriptor BuildRule(ValidationErrorCode code,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? overrides)
    {
        var meta = RuleMetadataProvider.Get(code);
        var effectiveSeverity = GetEffectiveSeverity(code, overrides);
        return new SarifReportingDescriptor
        {
            Id = meta.Id,
            ShortDescription = new SarifMessage { Text = meta.Name },
            FullDescription = new SarifMessage { Text = meta.Description },
            DefaultConfiguration = new SarifDefaultConfiguration
            {
                Level = MapToSarifLevel(effectiveSeverity)
            }
        };
    }

    private static SarifResult BuildResult(string filePath, ValidationError error,
        IReadOnlyDictionary<ValidationErrorCode, RuleSeverity?>? overrides)
    {
        var effectiveSeverity = GetEffectiveSeverity(error.Code, overrides);
        return new SarifResult
        {
            RuleId = error.Code.ToCode(),
            Level = MapToSarifLevel(effectiveSeverity),
            Message = new SarifMessage { Text = error.Message },
            Locations =
            [
                new SarifLocation
                {
                    PhysicalLocation = new SarifPhysicalLocation
                    {
                        ArtifactLocation = new SarifArtifactLocation { Uri = filePath },
                        Region = error.Line.HasValue ? new SarifRegion { StartLine = error.Line.Value } : null
                    }
                }
            ]
        };
    }

    private static string MapToSarifLevel(RuleSeverity severity) => severity switch
    {
        RuleSeverity.BLOCKER or RuleSeverity.CRITICAL or RuleSeverity.MAJOR => "error",
        RuleSeverity.MINOR => "warning",
        RuleSeverity.INFO => "note",
        _ => "error"
    };
}
