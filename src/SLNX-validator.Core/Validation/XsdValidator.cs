using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

public sealed class XsdValidator : IXsdValidator
{
    // Source: https://github.com/microsoft/vs-solutionpersistence/blob/main/src/Microsoft.VisualStudio.SolutionPersistence/Serializer/Xml/Slnx.xsd
    private const string XsdResourceName = "JulianVerdurmen.SlnxValidator.Slnx.xsd";

    public async Task ValidateAsync(string slnxContent, ValidationResult result, CancellationToken cancellationToken)
    {
        var schemas = new XmlSchemaSet();

        await using var xsdStream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream(XsdResourceName) ?? throw new InvalidOperationException("XSD file not found");

        using var xsdReader = XmlReader.Create(xsdStream);
        schemas.Add(null, xsdReader);

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemas,
            Async = true,
        };

        settings.ValidationEventHandler += (_, e) =>
        {
            var line = e.Exception?.LineNumber is int l and > 0 ? (int?)l : null;
            var column = e.Exception?.LinePosition is int c and > 0 ? (int?)c : null;
            result.AddError(ValidationErrorCode.XsdViolation, e.Message, line: line, column: column);
        };

        using var stringReader = new StringReader(slnxContent);
        using var xmlReader = XmlReader.Create(stringReader, settings);

        while (await xmlReader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
