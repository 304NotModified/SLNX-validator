namespace JulianVerdurmen.SlnxValidator.Core.ValidationResults;

public sealed record ValidationError(
    ValidationErrorCode Code,
    string Message,
    string? File = null,
    int? Line = null,
    int? Column = null);
