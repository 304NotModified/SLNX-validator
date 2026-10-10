using System.Text.Json;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.Reporting;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.SarifReporting;

public sealed class SarifReporter(IFileSystem fileSystem) : ReporterBase(fileSystem), ISarifReporter
{
    private const string SarifSchema = "https://json.schemastore.org/sarif-2.1.0.json";
    private const string SarifVersion = "2.1.0";
    private const string ToolName = "slnx-validator";
    private static readonly string ToolInformationUri = ThisAssembly.Info.RepositoryUrl;

    public override async Task WriteReportAsync(ReportResults results, Stream outputStream)
    {
        var usedCodes = results.UsedCodes;

        var rules = usedCodes
            .Select(c => BuildRule(c, results.Overrides))
            .ToList();

        var sarifResults = results.Results
            .SelectMany(r => r.Errors
                .Where(e => !results.Overrides.IsIgnored(e.Code))
                .Select(e => BuildResult(r.File, e, results.Overrides)))
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

    private static SarifReportingDescriptor BuildRule(ValidationErrorCode code, SeverityOverrides overrides)
    {
        var resolved = RuleProvider.Resolve(code, overrides);
        return new SarifReportingDescriptor
        {
            Id = resolved.Id,
            ShortDescription = new SarifMessage { Text = resolved.Name },
            FullDescription = new SarifMessage { Text = resolved.Description },
            DefaultConfiguration = new SarifDefaultConfiguration
            {
                Level = MapToSarifLevel(resolved.EffectiveSeverity)
            }
        };
    }

    private static SarifResult BuildResult(string filePath, ValidationError error, SeverityOverrides overrides)
    {
        var effectiveSeverity = RuleProvider.GetEffectiveSeverity(error.Code, overrides);
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
