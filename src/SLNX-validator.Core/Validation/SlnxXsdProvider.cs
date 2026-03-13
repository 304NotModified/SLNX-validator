using System.Reflection;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

internal sealed class SlnxXsdProvider : ISlnxXsdProvider
{
    // Source: https://github.com/microsoft/vs-solutionpersistence/blob/main/src/Microsoft.VisualStudio.SolutionPersistence/Serializer/Xml/Slnx.xsd
    private const string XsdResourceName = "JulianVerdurmen.SlnxValidator.Slnx.xsd";

    public Stream GetXsdStream()
    {
        return Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream(XsdResourceName) ?? throw new InvalidOperationException($"XSD resource '{XsdResourceName}' not found in assembly");
    }
}
