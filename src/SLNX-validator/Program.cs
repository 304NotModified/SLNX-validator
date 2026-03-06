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

        var rootCommand = new RootCommand("Validates .slnx solution files.")
        {
            inputArgument
        };

        var services = new ServiceCollection()
            .AddSlnxValidator()
            .AddSingleton<ValidationCollector>()
            .AddSingleton<ValidatorRunner>()
            .BuildServiceProvider();

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = parseResult.GetValue(inputArgument);
            return await services.GetRequiredService<ValidatorRunner>().RunAsync(input!, cancellationToken);
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
