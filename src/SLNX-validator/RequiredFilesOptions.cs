namespace JulianVerdurmen.SlnxValidator;

/// <summary>
/// Bundled options for the <c>--required-files</c> feature passed to
/// <see cref="ValidationCollector.CollectAsync"/>.
/// </summary>
/// <param name="MatchedPaths">
/// Absolute disk paths that were matched by <see cref="Pattern"/>.
/// An empty list with a wildcard pattern means no files to check — not an error.
/// An empty list with a literal (non-wildcard) pattern means the file does not exist — emits <c>SLNX020</c>.
/// <see langword="null"/> means the <c>--required-files</c> option was not used.
/// </param>
/// <param name="Pattern">The raw semicolon-separated pattern string supplied by the user.</param>
internal sealed record RequiredFilesOptions(
    IReadOnlyList<string>? MatchedPaths,
    string? Pattern)
{
    /// <summary>
    /// Returns <see langword="true"/> when <see cref="Pattern"/> contains wildcard characters
    /// (<c>*</c> or <c>?</c>), meaning zero matches is not an error.
    /// </summary>
    internal bool HasWildcard => Pattern is not null && (Pattern.Contains('*') || Pattern.Contains('?'));
}
