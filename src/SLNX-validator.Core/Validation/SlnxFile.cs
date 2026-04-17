using System.Xml;
using System.Xml.Linq;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

/// <summary>
/// Represents a parsed .slnx solution file, exposing its typed domain model and
/// the resolved absolute paths of all referenced files.
/// </summary>
public sealed class SlnxFile
{
    /// <summary>The directory that contains the .slnx file.</summary>
    public string SlnxDirectory { get; }

    /// <summary>The raw XML content of the .slnx file.</summary>
    public string OriginalContent { get; }

    /// <summary>The typed domain model of the parsed solution.</summary>
    public SlnxSolution Solution { get; }

    /// <summary>Absolute, normalised paths for every <c>&lt;File&gt;</c> entry in the solution.</summary>
    public IReadOnlyList<string> Files { get; }

    private SlnxFile(string slnxDirectory, string originalContent, SlnxSolution solution, IReadOnlyList<string> files)
    {
        SlnxDirectory = slnxDirectory;
        OriginalContent = originalContent;
        Solution = solution;
        Files = files;
    }

    /// <summary>
    /// Parses <paramref name="slnxContent"/> into a typed domain model.  Relative file paths are
    /// resolved against <paramref name="slnxDirectory"/>.
    /// </summary>
    /// <returns>The parsed <see cref="SlnxFile"/>, or <see langword="null"/> when the XML is malformed.</returns>
    public static SlnxFile? Parse(string slnxContent, string slnxDirectory)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Parse(slnxContent, LoadOptions.SetLineInfo);
        }
        catch (Exception)
        {
            return null;
        }

        return FromDocument(doc, slnxContent, slnxDirectory);
    }

    /// <summary>
    /// Builds a <see cref="SlnxFile"/> from an already-parsed <see cref="XDocument"/>.
    /// Use this overload when the caller has already performed XML parsing (e.g. for error reporting).
    /// </summary>
    public static SlnxFile FromDocument(XDocument doc, string originalContent, string slnxDirectory)
    {
        var solution = ParseSolution(doc);
        var files = ComputeAbsoluteFiles(solution, slnxDirectory);
        return new SlnxFile(slnxDirectory, originalContent, solution, files);
    }

    private static SlnxLineInfo? GetLineInfo(XObject obj)
    {
        if (obj is IXmlLineInfo li && li.HasLineInfo())
            return new SlnxLineInfo(li.LineNumber, li.LinePosition);
        return null;
    }

    private static SlnxSolution ParseSolution(XDocument doc)
    {
        var root = doc.Root;
        if (root is null)
            return new SlnxSolution(null, null, [], [], [], null);

        return new SlnxSolution(
            description: root.Attribute("Description")?.Value,
            version: root.Attribute("Version")?.Value,
            files: root.Elements("File").Select(ParseFileEntry).ToList(),
            projects: root.Elements("Project").Select(ParseProject).ToList(),
            folders: root.Elements("Folder").Select(ParseFolder).ToList(),
            lineInfo: GetLineInfo(root));
    }

    private static SlnxFolder ParseFolder(XElement el)
        => new(
            name: el.Attribute("Name")?.Value ?? string.Empty,
            files: el.Elements("File").Select(ParseFileEntry).ToList(),
            projects: el.Elements("Project").Select(ParseProject).ToList(),
            folders: el.Elements("Folder").Select(ParseFolder).ToList(),
            lineInfo: GetLineInfo(el));

    private static SlnxProject ParseProject(XElement el)
        => new(
            path: el.Attribute("Path")?.Value ?? string.Empty,
            type: el.Attribute("Type")?.Value,
            displayName: el.Attribute("DisplayName")?.Value,
            lineInfo: GetLineInfo(el));

    private static SlnxFileEntry ParseFileEntry(XElement el)
        => new(
            path: el.Attribute("Path")?.Value ?? string.Empty,
            lineInfo: GetLineInfo(el));

    private static IReadOnlyList<string> ComputeAbsoluteFiles(SlnxSolution solution, string slnxDirectory)
    {
        var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in solution.AllFiles().Where(entry => !string.IsNullOrEmpty(entry.Path)).Select(entry => entry.Path))
        {
            var fullPath = Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(slnxDirectory, path));

            refs.Add(fullPath);
        }

        return [.. refs];
    }
}
