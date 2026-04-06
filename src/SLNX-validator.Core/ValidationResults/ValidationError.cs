namespace JulianVerdurmen.SlnxValidator.Core.ValidationResults;

public sealed record ValidationError(
    ValidationErrorCode Code,
    string Message,
    string? ShortMessage = null,
    string? File = null,
    int? Line = null,
    int? Column = null);
