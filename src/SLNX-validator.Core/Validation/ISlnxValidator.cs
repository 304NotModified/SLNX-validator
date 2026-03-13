using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

public interface ISlnxValidator
{
    Task<ValidationResult> ValidateAsync(string slnxContent, string slnxDirectory, CancellationToken cancellationToken = default);
}
