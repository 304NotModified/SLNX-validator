namespace JulianVerdurmen.SlnxValidator.Core.Validation;

/// <summary>A <c>&lt;File&gt;</c> entry inside a .slnx solution.</summary>
public sealed class SlnxFileEntry
{
    /// <summary>The path as written in the .slnx file (may be relative or absolute).</summary>
    public string Path { get; }

    /// <summary>1-based line number of the element in the source, or <see langword="null"/> when unavailable.</summary>
    public int? Line { get; }

    /// <summary>1-based column number of the element in the source, or <see langword="null"/> when unavailable.</summary>
    public int? Column { get; }

    internal SlnxFileEntry(string path, int? line, int? column)
    {
        Path = path;
        Line = line;
        Column = column;
    }
}

/// <summary>A <c>&lt;Project&gt;</c> element in a .slnx solution.</summary>
public sealed class SlnxProject
{
    /// <summary>The project path (relative or absolute).</summary>
    public string Path { get; }

    /// <summary>Optional project type.</summary>
    public string? Type { get; }

    /// <summary>Optional display name.</summary>
    public string? DisplayName { get; }

    /// <summary>1-based line number of the element in the source, or <see langword="null"/> when unavailable.</summary>
    public int? Line { get; }

    /// <summary>1-based column number of the element in the source, or <see langword="null"/> when unavailable.</summary>
    public int? Column { get; }

    internal SlnxProject(string path, string? type, string? displayName, int? line, int? column)
    {
        Path = path;
        Type = type;
        DisplayName = displayName;
        Line = line;
        Column = column;
    }
}

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

    /// <summary>1-based line number of the element in the source, or <see langword="null"/> when unavailable.</summary>
    public int? Line { get; }

    /// <summary>1-based column number of the element in the source, or <see langword="null"/> when unavailable.</summary>
    public int? Column { get; }

    internal SlnxFolder(string name, IReadOnlyList<SlnxFileEntry> files, IReadOnlyList<SlnxProject> projects, IReadOnlyList<SlnxFolder> folders, int? line, int? column)
    {
        Name = name;
        Files = files;
        Projects = projects;
        Folders = folders;
        Line = line;
        Column = column;
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

    /// <summary>1-based line number of the root element, or <see langword="null"/> when unavailable.</summary>
    public int? Line { get; }

    /// <summary>1-based column number of the root element, or <see langword="null"/> when unavailable.</summary>
    public int? Column { get; }

    internal SlnxSolution(string? description, string? version, IReadOnlyList<SlnxFileEntry> files, IReadOnlyList<SlnxProject> projects, IReadOnlyList<SlnxFolder> folders, int? line, int? column)
    {
        Description = description;
        Version = version;
        Files = files;
        Projects = projects;
        Folders = folders;
        Line = line;
        Column = column;
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
