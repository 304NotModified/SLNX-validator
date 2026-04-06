using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Reporting;

public static class RuleMetadataProvider
{
    public static RuleMetadata Get(ValidationErrorCode code) => code switch
    {
        ValidationErrorCode.FileNotFound => new(
            code.ToCode(),
            "Input file not found",
            "The specified .slnx file does not exist.",
            RuleSeverity.MAJOR),

        ValidationErrorCode.InvalidExtension => new(
            code.ToCode(),
            "Invalid file extension",
            "The input file does not have a .slnx extension.",
            RuleSeverity.MINOR),

        ValidationErrorCode.NotATextFile => new(
            code.ToCode(),
            "File is not a text file",
            "The file is binary and cannot be parsed as XML.",
            RuleSeverity.MAJOR),

        ValidationErrorCode.InvalidXml => new(
            code.ToCode(),
            "Invalid XML",
            "The .slnx file is not valid XML.",
            RuleSeverity.MAJOR),

        ValidationErrorCode.ReferencedFileNotFound => new(
            code.ToCode(),
            "Referenced file not found",
            "A file referenced in a <File Path=\"...\"> element does not exist on disk.",
            RuleSeverity.MAJOR),

        ValidationErrorCode.InvalidWildcardUsage => new(
            code.ToCode(),
            "Invalid wildcard usage",
            "A <File Path=\"...\"> element contains a wildcard pattern, which is not supported.",
            RuleSeverity.MINOR),

        ValidationErrorCode.XsdViolation => new(
            code.ToCode(),
            "XSD schema violation",
            "The XML structure violates the .slnx schema.",
            RuleSeverity.MAJOR),

        ValidationErrorCode.RequiredFileDoesntExistOnSystem => new(
            code.ToCode(),
            "Required file does not exist on the system",
            "A file required by '--required-files' does not exist on the file system.",
            RuleSeverity.MAJOR),

        ValidationErrorCode.RequiredFileNotReferencedInSolution => new(
            code.ToCode(),
            "Required file not referenced in solution",
            "A file required by '--required-files' exists on the file system but is not referenced as a <File> element in the solution.",
            RuleSeverity.MAJOR),

        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };
}
