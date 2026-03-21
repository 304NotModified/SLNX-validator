using System.CommandLine;
using JulianVerdurmen.SlnxValidator.Core;
using JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;
using Microsoft.Extensions.DependencyInjection;

namespace JulianVerdurmen.SlnxValidator;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = "Path to a .slnx file, directory, glob pattern (e.g. src/*.slnx), or comma-separated combination."
        };

        var sonarqubeReportOption = new Option<string?>("--sonarqube-report-file")
        {
            Description = "Write a SonarQube generic issue report to the specified file path."
        };

        var continueOnErrorOption = new Option<bool>("--continue-on-error")
        {
            Description = "Continue and exit with code 0 even when validation errors are found."
        };

        var requiredFilesOption = new Option<string?>("--required-files")
        {
            Description = "Semicolon-separated glob patterns for files that must exist on disk and be referenced as <File> elements in the solution."
        };

        var blockerOption = new Option<string?>("--blocker")
        {
            Description = "Comma-separated codes (or * for all) to treat as BLOCKER severity (causes exit code 1)."
        };

        var criticalOption = new Option<string?>("--critical")
        {
            Description = "Comma-separated codes (or * for all) to treat as CRITICAL severity (causes exit code 1)."
        };

        var majorOption = new Option<string?>("--major")
        {
            Description = "Comma-separated codes (or * for all) to treat as MAJOR severity (causes exit code 1)."
        };

        var minorOption = new Option<string?>("--minor")
        {
            Description = "Comma-separated codes (or * for all) to treat as MINOR severity (shown, but does not cause exit code 1)."
        };

        var infoOption = new Option<string?>("--info")
        {
            Description = "Comma-separated codes (or * for all) to treat as INFO severity (shown, but does not cause exit code 1)."
        };

        var ignoreOption = new Option<string?>("--ignore")
        {
            Description = "Comma-separated codes (or * for all) to suppress entirely (not shown, not in SonarQube report)."
        };

        var rootCommand = new RootCommand("Validates .slnx solution files.")
        {
            inputArgument,
            sonarqubeReportOption,
            continueOnErrorOption,
            requiredFilesOption,
            blockerOption,
            criticalOption,
            majorOption,
            minorOption,
            infoOption,
            ignoreOption
        };

        var services = new ServiceCollection()
            .AddSlnxValidator()
            .AddSingleton<ValidationCollector>()
            .AddSingleton<ValidatorRunner>()
            .BuildServiceProvider();

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var options = new ValidatorRunnerOptions(
                Input: parseResult.GetValue(inputArgument)!,
                SonarqubeReportPath: parseResult.GetValue(sonarqubeReportOption),
                ContinueOnError: parseResult.GetValue(continueOnErrorOption),
                RequiredFilesPattern: parseResult.GetValue(requiredFilesOption),
                WorkingDirectory: Environment.CurrentDirectory,
                SeverityOverrides: ParseSeverityOverrides(
                    parseResult.GetValue(blockerOption),
                    parseResult.GetValue(criticalOption),
                    parseResult.GetValue(majorOption),
                    parseResult.GetValue(minorOption),
                    parseResult.GetValue(infoOption),
                    parseResult.GetValue(ignoreOption)));

            return await services.GetRequiredService<ValidatorRunner>().RunAsync(options, cancellationToken);
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    internal static IReadOnlyDictionary<ValidationErrorCode, SonarRuleSeverity?> ParseSeverityOverrides(
        string? blocker, string? critical, string? major, string? minor, string? info, string? ignore)
    {
        var result = new Dictionary<ValidationErrorCode, SonarRuleSeverity?>();

        // Pass 1: wildcards only (lowest priority — expanded first so specific codes can overwrite)
        ParseInto(blocker,  SonarRuleSeverity.BLOCKER,  result, wildcardOnly: true);
        ParseInto(critical, SonarRuleSeverity.CRITICAL, result, wildcardOnly: true);
        ParseInto(major,    SonarRuleSeverity.MAJOR,    result, wildcardOnly: true);
        ParseInto(minor,    SonarRuleSeverity.MINOR,    result, wildcardOnly: true);
        ParseInto(info,     SonarRuleSeverity.INFO,     result, wildcardOnly: true);
        ParseInto(ignore,   null,                       result, wildcardOnly: true);

        // Pass 2: specific codes (highest priority — overwrite wildcards from pass 1)
        ParseInto(blocker,  SonarRuleSeverity.BLOCKER,  result, wildcardOnly: false);
        ParseInto(critical, SonarRuleSeverity.CRITICAL, result, wildcardOnly: false);
        ParseInto(major,    SonarRuleSeverity.MAJOR,    result, wildcardOnly: false);
        ParseInto(minor,    SonarRuleSeverity.MINOR,    result, wildcardOnly: false);
        ParseInto(info,     SonarRuleSeverity.INFO,     result, wildcardOnly: false);
        ParseInto(ignore,   null,                       result, wildcardOnly: false);

        return result;
    }

    private static void ParseInto(string? input, SonarRuleSeverity? severity,
        Dictionary<ValidationErrorCode, SonarRuleSeverity?> target, bool wildcardOnly)
    {
        if (input is null) return;

        if (input.Trim() == "*")
        {
            if (wildcardOnly)
            {
                foreach (var code in Enum.GetValues<ValidationErrorCode>())
                    target[code] = severity;
            }
            return;
        }

        if (wildcardOnly) return;

        foreach (var raw in input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (raw == "*") continue;
            target[ParseCode(raw)] = severity;
        }
    }

    private static ValidationErrorCode ParseCode(string raw)
    {
        if (raw.StartsWith("SLNX", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(raw.AsSpan(4), out var num) &&
            Enum.IsDefined(typeof(ValidationErrorCode), num))
        {
            return (ValidationErrorCode)num;
        }

        if (Enum.TryParse<ValidationErrorCode>(raw, ignoreCase: true, out var code))
            return code;

        throw new InvalidOperationException($"Unknown validation code: '{raw}'. Use the SLNX-prefixed code (e.g. SLNX011) or the enum name (e.g. ReferencedFileNotFound).");
    }
}
