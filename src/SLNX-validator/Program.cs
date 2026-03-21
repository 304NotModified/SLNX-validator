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
            var input = parseResult.GetValue(inputArgument);
            var sonarqubeReport = parseResult.GetValue(sonarqubeReportOption);
            var continueOnError = parseResult.GetValue(continueOnErrorOption);
            var requiredFiles = parseResult.GetValue(requiredFilesOption);
            return await services.GetRequiredService<ValidatorRunner>()
                .RunAsync(input!, sonarqubeReport, continueOnError, requiredFiles, Environment.CurrentDirectory, cancellationToken);
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
