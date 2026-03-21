using System.CommandLine;
using JulianVerdurmen.SlnxValidator.Core;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
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
            Description = "Semicolon-separated glob patterns for required files and directories. The tool exits with code 2 if any pattern produces no match or a matched path does not exist."
        };

        var rootCommand = new RootCommand("Validates .slnx solution files.")
        {
            inputArgument,
            sonarqubeReportOption,
            continueOnErrorOption,
            requiredFilesOption
        };

        var services = new ServiceCollection()
            .AddSlnxValidator()
            .AddSingleton<ValidationCollector>()
            .AddSingleton<ValidatorRunner>()
            .BuildServiceProvider();

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var requiredFiles = parseResult.GetValue(requiredFilesOption);
            IReadOnlyList<string>? matchedRequiredPaths = null;

            if (requiredFiles is not null)
            {
                // Pre-check: glob patterns must match at least one file on disk.
                matchedRequiredPaths = RequiredFilesChecker.ResolveMatchedPaths(requiredFiles, Environment.CurrentDirectory);
                if (matchedRequiredPaths.Count == 0)
                {
                    await Console.Error.WriteLineAsync($"[SLNX020] Required files check failed: no files matched the patterns: {requiredFiles}");
                    return 2;
                }
            }

            var input = parseResult.GetValue(inputArgument);
            var sonarqubeReport = parseResult.GetValue(sonarqubeReportOption);
            var continueOnError = parseResult.GetValue(continueOnErrorOption);
            var runResult = await services.GetRequiredService<ValidatorRunner>().RunAsync(input!, sonarqubeReport, continueOnError, cancellationToken);

            if (matchedRequiredPaths is not null)
            {
                // Last check: every required file must be referenced as a <File> in the .slnx.
                var slnxFiles = services.GetRequiredService<ISlnxFileResolver>().Resolve(input!);
                var slnxCheckResult = await RequiredFilesChecker.CheckInSlnxAsync(matchedRequiredPaths, slnxFiles);
                if (slnxCheckResult != 0)
                    return slnxCheckResult;
            }

            return runResult;
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
