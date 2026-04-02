using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator;

internal sealed class ValidationCollector(IFileSystem fileSystem, ISlnxValidator validator, IRequiredFilesChecker requiredFilesChecker)
{
    public async Task<IReadOnlyList<FileValidationResult>> CollectAsync(
        IReadOnlyList<string> files,
        RequiredFilesOptions? requiredFilesOptions,
        CancellationToken cancellationToken)
    {
        var results = new List<FileValidationResult>(files.Count);

        foreach (var file in files)
        {
            if (!fileSystem.FileExists(file))
            {
                results.Add(Error(file, ValidationErrorCode.FileNotFound, $"File not found: {file}"));
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
            var validationResult = await validator.ValidateAsync(content, directory, cancellationToken);
            var result = validationResult.ValidationResult;

            var allErrors = result.Errors.ToList();

            if (requiredFilesOptions is not null)
            {
                var matched = requiredFilesOptions.MatchedPaths;
                if (matched is null || matched.Count == 0)
                {
                    allErrors.Add(new ValidationError(
                        ValidationErrorCode.RequiredFileDoesntExistOnSystem,
                        $"Required file does not exist on the system. No files matched: {requiredFilesOptions.Pattern}"));
                }
                else
                {
                    var slnxFile = validationResult.ParsedFile;
                    if (slnxFile is not null)
                        allErrors.AddRange(requiredFilesChecker.CheckInSlnx(matched, slnxFile));
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

    private static FileValidationResult Error(string file, ValidationErrorCode code, string message) =>
        new()
        {
            File = file,
            HasErrors = true,
            Errors = [new ValidationError(code, message)],
        };

    private bool IsBinaryFile(string path)
    {
        Span<byte> buffer = stackalloc byte[8000];
        using var stream = fileSystem.OpenRead(path);
        var bytesRead = stream.Read(buffer);
        return buffer[..bytesRead].Contains((byte)0);
    }
}

