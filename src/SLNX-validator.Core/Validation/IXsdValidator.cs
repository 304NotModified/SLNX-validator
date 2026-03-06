using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

public interface IXsdValidator
{
    Task ValidateAsync(string slnxContent, ValidationResult result, CancellationToken cancellationToken);
}
