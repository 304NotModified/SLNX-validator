namespace JulianVerdurmen.SlnxValidator.Core.ValidationResults;

public enum ValidationErrorCode
{
    // Input/file-level errors
    FileNotFound = 1,
    InvalidExtension = 2,
    NotATextFile = 3,

    // Content-level errors
    InvalidXml = 10,
    ReferencedFileNotFound = 11,
    InvalidWildcardUsage = 12,
    XsdViolation = 13,
}
