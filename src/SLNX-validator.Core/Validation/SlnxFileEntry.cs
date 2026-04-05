namespace JulianVerdurmen.SlnxValidator.Core.Validation;

/// <summary>A <c>&lt;File&gt;</c> entry inside a .slnx solution.</summary>
public sealed class SlnxFileEntry
{
    /// <summary>The path as written in the .slnx file (may be relative or absolute).</summary>
    public string Path { get; }

    /// <summary>The source location of this element, or <see langword="null"/> when unavailable.</summary>
    public SlnxLineInfo? LineInfo { get; }

    internal SlnxFileEntry(string path, SlnxLineInfo? lineInfo)
    {
        Path = path;
        LineInfo = lineInfo;
    }
}
