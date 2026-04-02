using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

public interface ISlnxValidator
{
    Task<ValidationResult> ValidateAsync(SlnxFile slnxFile, CancellationToken cancellationToken = default);
}
