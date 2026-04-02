using System.Xml.Linq;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

public interface ISlnxValidator
{
    Task<ValidationResult> ValidateAsync(XDocument doc, string slnxDirectory, CancellationToken cancellationToken = default);
}
