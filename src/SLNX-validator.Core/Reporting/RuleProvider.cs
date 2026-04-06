using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Reporting;

public static class RuleProvider
{
    private static readonly Dictionary<ValidationErrorCode, Rule> Rules = new()
    {
        [ValidationErrorCode.FileNotFound] = new(
            ValidationErrorCode.FileNotFound.ToCode(),
            "Input file not found",
            "The specified .slnx file does not exist.",
            RuleSeverity.MAJOR),

        [ValidationErrorCode.InvalidExtension] = new(
            ValidationErrorCode.InvalidExtension.ToCode(),
            "Invalid file extension",
            "The input file does not have a .slnx extension.",
            RuleSeverity.MINOR),

        [ValidationErrorCode.NotATextFile] = new(
            ValidationErrorCode.NotATextFile.ToCode(),
            "File is not a text file",
            "The file is binary and cannot be parsed as XML.",
            RuleSeverity.MAJOR),

        [ValidationErrorCode.InvalidXml] = new(
            ValidationErrorCode.InvalidXml.ToCode(),
            "Invalid XML",
            "The .slnx file is not valid XML.",
            RuleSeverity.MAJOR),

        [ValidationErrorCode.ReferencedFileNotFound] = new(
            ValidationErrorCode.ReferencedFileNotFound.ToCode(),
            "Referenced file not found",
            "A file referenced in a <File Path=\"...\"> element does not exist on disk.",
            RuleSeverity.MAJOR),

        [ValidationErrorCode.InvalidWildcardUsage] = new(
            ValidationErrorCode.InvalidWildcardUsage.ToCode(),
            "Invalid wildcard usage",
            "A <File Path=\"...\"> element contains a wildcard pattern, which is not supported.",
            RuleSeverity.MINOR),

        [ValidationErrorCode.XsdViolation] = new(
            ValidationErrorCode.XsdViolation.ToCode(),
            "XSD schema violation",
            "The XML structure violates the .slnx schema.",
            RuleSeverity.MAJOR),

        [ValidationErrorCode.RequiredFileDoesntExistOnSystem] = new(
            ValidationErrorCode.RequiredFileDoesntExistOnSystem.ToCode(),
            "Required file does not exist on the system",
            "A file required by '--required-files' does not exist on the file system.",
            RuleSeverity.MAJOR),

        [ValidationErrorCode.RequiredFileNotReferencedInSolution] = new(
            ValidationErrorCode.RequiredFileNotReferencedInSolution.ToCode(),
            "Required file not referenced in solution",
            "A file required by '--required-files' exists on the file system but is not referenced as a <File> element in the solution.",
            RuleSeverity.MAJOR),
    };

    public static Rule Get(ValidationErrorCode code)
    {
        if (Rules.TryGetValue(code, out var rule))
            return rule;
        throw new ArgumentOutOfRangeException(nameof(code), code, null);
    }
}
