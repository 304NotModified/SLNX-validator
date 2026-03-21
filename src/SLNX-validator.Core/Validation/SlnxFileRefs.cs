using System.Xml.Linq;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

/// <summary>
/// Represents the set of absolute file paths that are referenced as
/// <c>&lt;File Path="..."&gt;</c> elements inside a .slnx solution file.
/// </summary>
public sealed class SlnxFileRefs
{
    /// <summary>The directory that contains the .slnx file.</summary>
    public string SlnxDirectory { get; }

    /// <summary>Absolute, normalised paths for every <c>&lt;File&gt;</c> entry in the solution.</summary>
    public IReadOnlyList<string> AbsoluteFilePaths { get; }

    private SlnxFileRefs(string slnxDirectory, IReadOnlyList<string> absoluteFilePaths)
    {
        SlnxDirectory = slnxDirectory;
        AbsoluteFilePaths = absoluteFilePaths;
    }

    /// <summary>
    /// Parses <paramref name="slnxContent"/> and returns the resolved absolute paths of all
    /// <c>&lt;File Path="..."&gt;</c> elements.  Relative paths are resolved against
    /// <paramref name="slnxDirectory"/>.  Returns an empty set when the XML is malformed.
    /// </summary>
    public static SlnxFileRefs Parse(string slnxContent, string slnxDirectory)
    {
        var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

                refs.Add(fullPath);
            }
        }
        catch (Exception)
        {
            // Malformed XML is already reported by the XML validator.
        }

        return new SlnxFileRefs(slnxDirectory, [.. refs]);
    }
}
