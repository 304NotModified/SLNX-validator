using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator;

internal sealed class FileValidationResult
{
    public required string File { get; init; }
    public required bool HasErrors { get; init; }
    public required IReadOnlyList<ValidationError> Errors { get; init; }
}
