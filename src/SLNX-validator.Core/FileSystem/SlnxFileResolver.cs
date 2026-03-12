namespace JulianVerdurmen.SlnxValidator.Core.FileSystem;

internal sealed class SlnxFileResolver(IFileSystem fileSystem) : ISlnxFileResolver
{
    /// <summary>
    /// Resolves one or more .slnx file paths from the given input.
    /// Supports: single file, directory, glob mask, or comma-separated combination.
    /// </summary>
    public IReadOnlyList<string> Resolve(string input)
    {
        var results = new List<string>();

        foreach (var entry in input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            results.AddRange(ResolveEntry(entry));
        }

        return results;
    }

    private IEnumerable<string> ResolveEntry(string entry)
    {
        if (fileSystem.DirectoryExists(entry))
        {
            return fileSystem.GetFiles(entry, "*.slnx");
        }

        var directory = Path.GetDirectoryName(entry);
        var pattern = Path.GetFileName(entry);

        if (string.IsNullOrEmpty(directory))
        {
            directory = ".";
        }

        if (pattern.Contains('*') || pattern.Contains('?'))
        {
            if (!fileSystem.DirectoryExists(directory))
            {
                return [];
            }

            return fileSystem.GetFiles(directory, pattern);
        }

        return [entry];
    }
}
