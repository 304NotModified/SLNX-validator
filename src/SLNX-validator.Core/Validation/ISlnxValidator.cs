using System.Xml.Linq;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

public interface ISlnxValidator
{
    /// <summary>
    /// Validates the already-parsed .slnx document against the XSD schema and checks that all
    /// referenced files exist on disk. The raw <paramref name="slnxContent"/> is still required
    /// for XSD stream-based validation.
    /// </summary>
    Task<ValidationResult> ValidateAsync(XDocument doc, string slnxContent, string slnxDirectory, CancellationToken cancellationToken = default);
}
