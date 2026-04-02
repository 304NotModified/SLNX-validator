using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

/// <summary>
/// The result of a .slnx validation, containing both the validation errors and the parsed
/// <see cref="SlnxFile"/> so callers can reuse it without a second XML parse.
/// </summary>
public sealed record SlnxValidationResult(ValidationResult ValidationResult, SlnxFile? ParsedFile);
