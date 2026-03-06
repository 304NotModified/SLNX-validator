namespace JulianVerdurmen.SlnxValidator.Core.ValidationResults;

public static class ValidationErrorCodeExtensions
{
    public static string ToCode(this ValidationErrorCode code) =>
        $"SLNX{(int)code:D4}";
}
