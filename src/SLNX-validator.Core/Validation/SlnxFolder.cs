namespace JulianVerdurmen.SlnxValidator.Core.Validation;

/// <summary>A <c>&lt;Folder&gt;</c> element in a .slnx solution.</summary>
public sealed class SlnxFolder
{
    /// <summary>The folder name.</summary>
    public string Name { get; }

    /// <summary>File entries directly inside this folder.</summary>
    public IReadOnlyList<SlnxFileEntry> Files { get; }

    /// <summary>Projects directly inside this folder.</summary>
    public IReadOnlyList<SlnxProject> Projects { get; }

    /// <summary>Sub-folders directly inside this folder.</summary>
    public IReadOnlyList<SlnxFolder> Folders { get; }

    /// <summary>The source location of this element, or <see langword="null"/> when unavailable.</summary>
    public SlnxLineInfo? LineInfo { get; }

    internal SlnxFolder(string name, IReadOnlyList<SlnxFileEntry> files, IReadOnlyList<SlnxProject> projects, IReadOnlyList<SlnxFolder> folders, SlnxLineInfo? lineInfo)
    {
        Name = name;
        Files = files;
        Projects = projects;
        Folders = folders;
        LineInfo = lineInfo;
    }

    /// <summary>Enumerates all <see cref="SlnxFileEntry"/> objects in this folder and all nested folders.</summary>
    public IEnumerable<SlnxFileEntry> AllFiles()
    {
        foreach (var f in Files) yield return f;
        foreach (var sub in Folders)
            foreach (var f in sub.AllFiles())
                yield return f;
    }
}
