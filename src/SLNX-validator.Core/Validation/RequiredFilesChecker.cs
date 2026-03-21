using System.Xml.Linq;
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
        string slnxContent,
        string slnxDirectory)
    {
        var slnxFileRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var doc = XDocument.Parse(slnxContent);
            foreach (var fileElement in doc.Descendants("File"))
            {
                var path = fileElement.Attribute("Path")?.Value;
                if (path is null)
                    continue;

                var fullPath = Path.IsPathRooted(path)
                    ? Path.GetFullPath(path)
                    : Path.GetFullPath(Path.Combine(slnxDirectory, path));

                slnxFileRefs.Add(fullPath);
            }
        }
        catch (Exception)
        {
            // Malformed XML is already reported by the XML validator.
        }

        var errors = new List<ValidationError>();
        foreach (var requiredPath in requiredAbsolutePaths)
        {
            if (!slnxFileRefs.Contains(requiredPath))
            {
                var relativePath = Path.GetRelativePath(slnxDirectory, requiredPath).Replace('\\', '/');
                errors.Add(new ValidationError(
                    ValidationErrorCode.RequiredFileNotReferencedInSolution,
                    $"Required file is not referenced in the solution: {requiredPath}" +
                    $" — add: <File Path=\"{relativePath}\" />"));
            }
        }

        return errors;
    }
}
