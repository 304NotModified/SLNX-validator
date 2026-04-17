using JulianVerdurmen.SlnxValidator.Core.ValidationResults;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

internal sealed class RequiredFilesChecker : IRequiredFilesChecker
{
    /// <inheritdoc />
    public IReadOnlyList<string> ResolveMatchedPaths(string patternsRaw, string rootDirectory)
    {
        var patterns = patternsRaw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase, preserveFilterOrder: true);

        foreach (var pattern in patterns)
        {
            if (pattern.StartsWith('!'))
                matcher.AddExclude(pattern[1..]);
            else
                matcher.AddInclude(pattern);
        }

        var directoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(rootDirectory));
        var result = matcher.Execute(directoryInfo);

        return result.HasMatches
            ? result.Files.Select(f => Path.GetFullPath(Path.Combine(rootDirectory, f.Path))).ToList()
            : [];
    }

    /// <inheritdoc />
    public IReadOnlyList<ValidationError> CheckInSlnx(
        IReadOnlyList<string> requiredAbsolutePaths,
        SlnxFile slnxFile)
    {
        var errors = new List<ValidationError>();
        foreach (var requiredPath in requiredAbsolutePaths.Where(p => !slnxFile.Files.Contains(p, StringComparer.OrdinalIgnoreCase)))
        {
            var relativePath = Path.GetRelativePath(slnxFile.SlnxDirectory, requiredPath).Replace('\\', '/');
            errors.Add(new ValidationError(
                ValidationErrorCode.RequiredFileNotReferencedInSolution,
                $"Required file is not referenced in the solution: {requiredPath}" +
                $" — add: <File Path=\"{relativePath}\" />",
                ShortMessage: $"Required file is not referenced in the solution — add: <File Path=\"{relativePath}\" />"));
        }

        return errors;
    }
}

