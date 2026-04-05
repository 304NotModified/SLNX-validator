namespace JulianVerdurmen.SlnxValidator.Core.Validation;

/// <summary>A <c>&lt;Project&gt;</c> element in a .slnx solution.</summary>
public sealed class SlnxProject
{
    /// <summary>The project path (relative or absolute).</summary>
    public string Path { get; }

    /// <summary>Optional project type.</summary>
    public string? Type { get; }

    /// <summary>Optional display name.</summary>
    public string? DisplayName { get; }

    /// <summary>The source location of this element, or <see langword="null"/> when unavailable.</summary>
    public SlnxLineInfo? LineInfo { get; }

    internal SlnxProject(string path, string? type, string? displayName, SlnxLineInfo? lineInfo)
    {
        Path = path;
        Type = type;
        DisplayName = displayName;
        LineInfo = lineInfo;
    }
}
