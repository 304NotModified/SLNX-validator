using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace JulianVerdurmen.SlnxValidator.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSlnxValidator(this IServiceCollection services)
    {
        services.AddSingleton<IFileSystem, RealFileSystem>();
        services.AddSingleton<ISonarReporter, SonarReporter>();
        services.AddSingleton<ISlnxXsdProvider, SlnxXsdProvider>();
        services.AddSingleton<IXsdValidator, XsdValidator>();
        services.AddSingleton<ISlnxValidator, Validation.SlnxValidator>();
        services.AddSingleton<ISlnxFileResolver, SlnxFileResolver>();
        services.AddSingleton<IRequiredFilesChecker, Validation.RequiredFilesChecker>();
        return services;
    }
}
