using System.Xml.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace JulianVerdurmen.SlnxValidator;

internal static class RequiredFilesChecker
{
    /// <summary>
    /// Resolves glob patterns against <paramref name="rootDirectory"/> and returns
    /// the matched paths as absolute paths. Returns an empty list when nothing matches.
    /// </summary>
    public static IReadOnlyList<string> ResolveMatchedPaths(string patternsRaw, string rootDirectory)
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

    /// <summary>
    /// Pre-check: verifies that at least one file on disk matches the glob patterns.
    /// Returns exit code 2 when no files match; 0 otherwise.
    /// </summary>
    public static async Task<int> CheckAsync(string patternsRaw, string rootDirectory)
    {
        var matched = ResolveMatchedPaths(patternsRaw, rootDirectory);

        if (matched.Count == 0)
        {
            await Console.Error.WriteLineAsync($"[SLNX020] Required files check failed: no files matched the patterns: {patternsRaw}");
            return 2;
        }

        return 0;
    }

    /// <summary>
    /// Last check: verifies that every path in <paramref name="requiredAbsolutePaths"/> is
    /// referenced as a <c>&lt;File Path="..."&gt;</c> element in at least one of the
    /// <paramref name="slnxFilePaths"/> solution files. Paths in the .slnx are resolved
    /// relative to each solution file's directory before comparison.
    /// Returns exit code 2 when any required file is missing; 0 otherwise.
    /// </summary>
    public static async Task<int> CheckInSlnxAsync(IReadOnlyList<string> requiredAbsolutePaths, IReadOnlyList<string> slnxFilePaths)
    {
        // Collect all <File> paths declared in the .slnx files, normalised to absolute paths.
        var slnxFileRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var slnxFile in slnxFilePaths)
        {
            if (!File.Exists(slnxFile))
                continue;

            var slnxDir = Path.GetDirectoryName(slnxFile)!;
            try
            {
                var content = await File.ReadAllTextAsync(slnxFile);
                var doc = XDocument.Parse(content);
                foreach (var fileElement in doc.Descendants("File"))
                {
                    var path = fileElement.Attribute("Path")?.Value;
                    if (path is null)
                        continue;
                    var fullPath = Path.IsPathRooted(path)
                        ? Path.GetFullPath(path)
                        : Path.GetFullPath(Path.Combine(slnxDir, path));
                    slnxFileRefs.Add(fullPath);
                }
            }
            catch (Exception)
            {
                // Malformed .slnx files are already reported by the normal validation step.
            }
        }

        var missing = requiredAbsolutePaths.Where(p => !slnxFileRefs.Contains(p)).ToList();
        foreach (var m in missing)
            await Console.Error.WriteLineAsync($"[SLNX020] Required file not referenced in solution: {m}");

        return missing.Count > 0 ? 2 : 0;
    }
}
