using System.CommandLine;
using JulianVerdurmen.SlnxValidator.Core;
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
            .AddSingleton<SlnxCollector>()
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
                SeverityOverrides: SeverityOverridesParser.Parse(
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
}
