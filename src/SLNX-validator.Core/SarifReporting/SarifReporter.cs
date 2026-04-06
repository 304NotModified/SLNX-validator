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
    private const string ToolInformationUri = "https://github.com/304NotModified/SLNX-validator";

    public override async Task WriteReportAsync(ReportResults reportResults, Stream outputStream)
    {
        var usedCodes = reportResults.UsedCodes;

        var rules = usedCodes
            .Select(c => BuildRule(c, reportResults.Overrides))
            .ToList();

        var sarifResults = reportResults.Results
            .SelectMany(r => r.Errors
                .Where(e => !reportResults.Overrides.IsIgnored(e.Code))
                .Select(e => BuildResult(r.File, e, reportResults.Overrides)))
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
        var effectiveSeverity = overrides.GetEffectiveSeverity(error.Code);
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
