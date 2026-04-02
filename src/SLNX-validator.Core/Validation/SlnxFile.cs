using System.Xml.Linq;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

/// <summary>
/// Represents the set of absolute file paths that are referenced as
/// <c>&lt;File Path="..."&gt;</c> elements inside a .slnx solution file.
/// </summary>
public sealed class SlnxFile
{
    /// <summary>The directory that contains the .slnx file.</summary>
    public string SlnxDirectory { get; }

    /// <summary>The raw XML content of the .slnx file.</summary>
    public string OriginalContent { get; }

    /// <summary>The parsed XML document (with line info).</summary>
    public XDocument Document { get; }

    /// <summary>Absolute, normalised paths for every <c>&lt;File&gt;</c> entry in the solution.</summary>
    public IReadOnlyList<string> Files { get; }

    private SlnxFile(string slnxDirectory, string originalContent, XDocument document, IReadOnlyList<string> files)
    {
        SlnxDirectory = slnxDirectory;
        OriginalContent = originalContent;
        Document = document;
        Files = files;
    }

    /// <summary>
    /// Parses <paramref name="slnxContent"/> and returns the resolved absolute paths of all
    /// <c>&lt;File Path="..."&gt;</c> elements.  Relative paths are resolved against
    /// <paramref name="slnxDirectory"/>.
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

    public static SlnxFile FromDocument(XDocument doc, string originalContent, string slnxDirectory)
    {
        var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var fileElement in doc.Descendants("File"))
        {
            var path = fileElement.Attribute("Path")?.Value;
            if (path is null)
                continue;

            var fullPath = Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(slnxDirectory, path));

            refs.Add(fullPath);
        }

        return new SlnxFile(slnxDirectory, originalContent, doc, [.. refs]);
    }
}
