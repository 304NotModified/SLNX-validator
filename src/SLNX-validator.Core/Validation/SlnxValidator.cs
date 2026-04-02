using System.Xml;
using System.Xml.Linq;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

internal sealed class SlnxValidator(IFileSystem fileSystem, IXsdValidator xsdValidator) : ISlnxValidator
{
    /// <summary>
    /// Validates a .slnx file against the XSD schema and checks that all referenced files exist on disk.
    /// </summary>
    /// <param name="slnxContent">The raw XML content of the .slnx file.</param>
    /// <param name="slnxDirectory">The directory that contains the .slnx file, used to resolve relative paths.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<SlnxValidationResult> ValidateAsync(string slnxContent, string slnxDirectory, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        XDocument doc;
        try
        {
            doc = XDocument.Parse(slnxContent, LoadOptions.SetLineInfo);
        }
        catch (XmlException ex)
        {
            result.AddError(ValidationErrorCode.InvalidXml, $"Invalid XML: {ex.Message}", line: ex.LineNumber, column: ex.LinePosition);
            return new SlnxValidationResult(result, null);
        }

        await xsdValidator.ValidateAsync(slnxContent, result, cancellationToken);

        if (!result.IsValid)
        {
            return new SlnxValidationResult(result, null);
        }

        var slnxFile = SlnxFile.FromDocument(doc, slnxDirectory);
        ValidatePaths(doc, slnxDirectory, result);

        return new SlnxValidationResult(result, slnxFile);
    }

    private void ValidatePaths(XDocument doc, string slnxDirectory, ValidationResult result)
    {
        foreach (var file in doc.Descendants("File"))
        {
            var path = file.Attribute("Path")!.Value;
            var lineInfo = (IXmlLineInfo)file;
            var line = lineInfo.HasLineInfo() ? lineInfo.LineNumber : (int?)null;
            var column = lineInfo.HasLineInfo() ? lineInfo.LinePosition : (int?)null;

            if (path.Contains('*') || path.Contains('?'))
            {
                result.AddError(ValidationErrorCode.InvalidWildcardUsage,
                    $"Wildcard patterns are not supported in file paths: {path}", line: line, column: column);
                continue;
            }

            var fullPath = Path.IsPathRooted(path)
                ? path
                : Path.Combine(slnxDirectory, path);

            if (!fileSystem.FileExists(fullPath))
            {
                result.AddError(ValidationErrorCode.ReferencedFileNotFound, $"File not found: {path}", line: line, column: column);
            }
        }
    }
}
