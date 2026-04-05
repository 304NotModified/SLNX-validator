using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

internal sealed class SlnxValidator(IFileSystem fileSystem, IXsdValidator xsdValidator) : ISlnxValidator
{
    public async Task<ValidationResult> ValidateAsync(SlnxFile slnxFile, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        await xsdValidator.ValidateAsync(slnxFile.OriginalContent, result, cancellationToken);

        if (!result.IsValid)
        {
            return result;
        }

        ValidatePaths(slnxFile, result);

        return result;
    }

    private void ValidatePaths(SlnxFile slnxFile, ValidationResult result)
    {
        foreach (var fileEntry in slnxFile.Solution.AllFiles())
        {
            var path = fileEntry.Path;
            var line = fileEntry.Line;
            var column = fileEntry.Column;

            if (path.Contains('*') || path.Contains('?'))
            {
                result.AddError(ValidationErrorCode.InvalidWildcardUsage,
                    $"Wildcard patterns are not supported in file paths: {path}", line: line, column: column);
                continue;
            }

            var fullPath = Path.IsPathRooted(path)
                ? path
                : Path.Combine(slnxFile.SlnxDirectory, path);

            if (!fileSystem.FileExists(fullPath))
            {
                result.AddError(ValidationErrorCode.ReferencedFileNotFound, $"File not found: {path}", line: line, column: column);
            }
        }
    }
}
