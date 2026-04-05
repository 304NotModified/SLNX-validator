namespace JulianVerdurmen.SlnxValidator.Core.Validation;

/// <summary>The root <c>&lt;Solution&gt;</c> element of a .slnx solution.</summary>
public sealed class SlnxSolution
{
    /// <summary>Optional solution description.</summary>
    public string? Description { get; }

    /// <summary>Optional solution version.</summary>
    public string? Version { get; }

    /// <summary>File entries directly under the root <c>&lt;Solution&gt;</c> element.</summary>
    public IReadOnlyList<SlnxFileEntry> Files { get; }

    /// <summary>Top-level projects.</summary>
    public IReadOnlyList<SlnxProject> Projects { get; }

    /// <summary>Top-level folders.</summary>
    public IReadOnlyList<SlnxFolder> Folders { get; }

    /// <summary>The source location of the root element, or <see langword="null"/> when unavailable.</summary>
    public SlnxLineInfo? LineInfo { get; }

    internal SlnxSolution(string? description, string? version, IReadOnlyList<SlnxFileEntry> files, IReadOnlyList<SlnxProject> projects, IReadOnlyList<SlnxFolder> folders, SlnxLineInfo? lineInfo)
    {
        Description = description;
        Version = version;
        Files = files;
        Projects = projects;
        Folders = folders;
        LineInfo = lineInfo;
    }

    /// <summary>
    /// Enumerates all <see cref="SlnxFileEntry"/> objects in the solution — both those directly under
    /// <c>&lt;Solution&gt;</c> and those recursively nested inside folders.
    /// </summary>
    public IEnumerable<SlnxFileEntry> AllFiles()
    {
        foreach (var f in Files) yield return f;
        foreach (var folder in Folders)
            foreach (var f in folder.AllFiles())
                yield return f;
    }
}
