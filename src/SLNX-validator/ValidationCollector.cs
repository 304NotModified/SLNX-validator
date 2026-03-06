using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;
using CoreSlnxValidator = JulianVerdurmen.SlnxValidator.Core.Validation.SlnxValidator;

namespace JulianVerdurmen.SlnxValidator;

internal sealed class ValidationCollector(IFileSystem fileSystem, CoreSlnxValidator validator)
{
    public async Task<IReadOnlyList<FileValidationResult>> CollectAsync(IReadOnlyList<string> files, CancellationToken cancellationToken)
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

            var content = await File.ReadAllTextAsync(file, cancellationToken);
            var directory = Path.GetDirectoryName(file)!;
            var result = await validator.ValidateAsync(content, directory, cancellationToken);

            results.Add(new FileValidationResult
            {
                File = file,
                HasErrors = !result.IsValid,
                Errors = result.Errors,
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

    private static bool IsBinaryFile(string path)
    {
        Span<byte> buffer = stackalloc byte[8000];
        using var stream = File.OpenRead(path);
        var bytesRead = stream.Read(buffer);
        return buffer[..bytesRead].Contains((byte)0);
    }
}
