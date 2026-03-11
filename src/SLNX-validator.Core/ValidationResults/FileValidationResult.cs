namespace JulianVerdurmen.SlnxValidator.Core.ValidationResults;

public sealed class FileValidationResult
{
    public required string File { get; init; }
    public required bool HasErrors { get; init; }
    public required IReadOnlyList<ValidationError> Errors { get; init; }
}