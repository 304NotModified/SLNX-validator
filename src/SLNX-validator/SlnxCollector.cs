using System.Xml;
using System.Xml.Linq;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator;

internal sealed class SlnxCollector(IFileSystem fileSystem, ISlnxFileResolver fileResolver, ISlnxValidator validator, IRequiredFilesChecker requiredFilesChecker)
{
    public async Task<IReadOnlyList<FileValidationResult>> CollectAsync(
        string input,
        RequiredFilesOptions? requiredFilesOptions,
        CancellationToken cancellationToken)
    {
        var files = fileResolver.Resolve(input);
        var results = new List<FileValidationResult>(files.Count);

        foreach (var file in files)
        {
            if (!fileSystem.FileExists(file))
            {
                results.Add(Error(file, ValidationErrorCode.FileNotFound, $"File not found: {file}",
                    shortMessage: "The specified .slnx file does not exist"));
                continue;
            }

            if (!string.Equals(Path.GetExtension(file), ".slnx", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(Error(file, ValidationErrorCode.InvalidExtension,
                    $"Expected a .slnx file, but got: {Path.GetFileName(file)}"));
                continue;
            }

            if (IsBinaryFile(file))
            {
                results.Add(Error(file, ValidationErrorCode.NotATextFile,
                    $"File is not a text file: {Path.GetFileName(file)}"));
                continue;
            }

            var content = await fileSystem.ReadAllTextAsync(file, cancellationToken);
            var directory = Path.GetDirectoryName(file)!;

            XDocument doc;
            try
            {
                doc = XDocument.Parse(content, LoadOptions.SetLineInfo);
            }
            catch (XmlException ex)
            {
                results.Add(Error(file, ValidationErrorCode.InvalidXml, $"Invalid XML: {ex.Message}",
                    line: ex.LineNumber, column: ex.LinePosition));
                continue;
            }

            var slnxFile = SlnxFile.FromDocument(doc, content, directory);
            var validationResult = await validator.ValidateAsync(slnxFile, cancellationToken);
            var allErrors = validationResult.Errors.ToList();

            if (requiredFilesOptions is not null)
            {
                var matched = requiredFilesOptions.MatchedPaths;
                if (matched is null || matched.Count == 0)
                {
                    if (!requiredFilesOptions.HasWildcard)
                    {
                        allErrors.Add(new ValidationError(
                            ValidationErrorCode.RequiredFileDoesntExistOnSystem,
                            $"Required file does not exist on the system. No files matched: {requiredFilesOptions.Pattern}"));
                    }
                }
                else
                {
                    var hasXsdErrors = allErrors.Any(e => e.Code == ValidationErrorCode.XsdViolation);
                    if (!hasXsdErrors)
                    {
                        allErrors.AddRange(requiredFilesChecker.CheckInSlnx(matched, slnxFile));
                    }
                }
            }

            results.Add(new FileValidationResult
            {
                File = file,
                HasErrors = allErrors.Count > 0,
                Errors = allErrors,
            });
        }

        return results;
    }

    private static FileValidationResult Error(string file, ValidationErrorCode code, string message,
        string? shortMessage = null, int? line = null, int? column = null) =>
        new()
        {
            File = file,
            HasErrors = true,
            Errors = [new ValidationError(code, message, ShortMessage: shortMessage, Line: line, Column: column)],
        };

    private bool IsBinaryFile(string path)
    {
        Span<byte> buffer = stackalloc byte[8000];
        using var stream = fileSystem.OpenRead(path);
        var bytesRead = stream.Read(buffer);
        return buffer[..bytesRead].Contains((byte)0);
    }
}
