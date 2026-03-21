namespace JulianVerdurmen.SlnxValidator;

/// <summary>
/// Bundled options for the <c>--required-files</c> feature passed to
/// <see cref="ValidationCollector.CollectAsync"/>.
/// </summary>
/// <param name="MatchedPaths">
/// Absolute disk paths that were matched by <see cref="Pattern"/>.
/// An empty list means the pattern matched no files.
/// <see langword="null"/> means the <c>--required-files</c> option was not used.
/// </param>
/// <param name="Pattern">The raw semicolon-separated pattern string supplied by the user.</param>
internal sealed record RequiredFilesOptions(
    IReadOnlyList<string>? MatchedPaths,
    string? Pattern);
