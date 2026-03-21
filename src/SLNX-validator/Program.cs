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
            var options = new ValidatorRunnerOptions(
                Input: parseResult.GetValue(inputArgument)!,
                SonarqubeReportPath: parseResult.GetValue(sonarqubeReportOption),
                ContinueOnError: parseResult.GetValue(continueOnErrorOption),
                RequiredFilesPattern: parseResult.GetValue(requiredFilesOption),
                WorkingDirectory: Environment.CurrentDirectory);

            return await services.GetRequiredService<ValidatorRunner>().RunAsync(options, cancellationToken);
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
