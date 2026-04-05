namespace JulianVerdurmen.SlnxValidator.Core.Validation;

/// <summary>The 1-based source location (line and column) of an element in a .slnx file.</summary>
public sealed class SlnxLineInfo
{
    /// <summary>1-based line number.</summary>
    public int Line { get; }

    /// <summary>1-based column number.</summary>
    public int Column { get; }

    internal SlnxLineInfo(int line, int column)
    {
        Line = line;
        Column = column;
    }
}
