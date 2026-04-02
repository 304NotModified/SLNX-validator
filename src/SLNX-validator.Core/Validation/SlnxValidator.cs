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
    /// <param name="doc">The already-parsed XML document (with line info).</param>
    /// <param name="slnxContent">The raw XML content, required for stream-based XSD validation.</param>
    /// <param name="slnxDirectory">The directory that contains the .slnx file, used to resolve relative paths.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<ValidationResult> ValidateAsync(XDocument doc, string slnxContent, string slnxDirectory, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        await xsdValidator.ValidateAsync(slnxContent, result, cancellationToken);

        if (!result.IsValid)
        {
            return result;
        }

        ValidatePaths(doc, slnxDirectory, result);

        return result;
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
