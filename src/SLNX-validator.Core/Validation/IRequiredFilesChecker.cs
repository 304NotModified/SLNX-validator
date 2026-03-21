using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

public interface IRequiredFilesChecker
{
    /// <summary>
    /// Resolves semicolon-separated glob patterns against <paramref name="rootDirectory"/>
    /// and returns the matched absolute paths. Returns an empty list when no files match.
    /// </summary>
    IReadOnlyList<string> ResolveMatchedPaths(string patternsRaw, string rootDirectory);

    /// <summary>
    /// Checks which of the <paramref name="requiredAbsolutePaths"/> are NOT present in
    /// <paramref name="slnxFile"/>.
    /// Returns a <see cref="ValidationError"/> for each missing file.
    /// </summary>
    IReadOnlyList<ValidationError> CheckInSlnx(
        IReadOnlyList<string> requiredAbsolutePaths,
        SlnxFile slnxFile);
}

