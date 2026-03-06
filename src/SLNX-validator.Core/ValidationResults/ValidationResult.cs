namespace JulianVerdurmen.SlnxValidator.Core.ValidationResults;

public sealed class ValidationResult
{
    private readonly List<ValidationError> _errors = [];

    public bool IsValid => _errors.Count == 0;
    public IReadOnlyList<ValidationError> Errors => _errors;

    internal void AddError(ValidationErrorCode code, string message, int? line = null, int? column = null) =>
        _errors.Add(new ValidationError(code, message, Line: line, Column: column));
}
