using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Reporting;

public static class RuleProvider
{
    private static Rule Create(ValidationErrorCode code, string name, string description,
        RuleSeverity severity = RuleSeverity.MAJOR) =>
        new(code.ToCode(), name, description, severity);

    private static readonly Dictionary<ValidationErrorCode, Rule> Rules = new()
    {
        [ValidationErrorCode.FileNotFound] = Create(
            ValidationErrorCode.FileNotFound,
            "Input file not found",
            "The specified .slnx file does not exist."),

        [ValidationErrorCode.InvalidExtension] = Create(
            ValidationErrorCode.InvalidExtension,
            "Invalid file extension",
            "The input file does not have a .slnx extension.",
            RuleSeverity.MINOR),

        [ValidationErrorCode.NotATextFile] = Create(
            ValidationErrorCode.NotATextFile,
            "File is not a text file",
            "The file is binary and cannot be parsed as XML."),

        [ValidationErrorCode.InvalidXml] = Create(
            ValidationErrorCode.InvalidXml,
            "Invalid XML",
            "The .slnx file is not valid XML."),

        [ValidationErrorCode.ReferencedFileNotFound] = Create(
            ValidationErrorCode.ReferencedFileNotFound,
            "Referenced file not found",
            "A file referenced in a <File Path=\"...\"> element does not exist on disk."),

        [ValidationErrorCode.InvalidWildcardUsage] = Create(
            ValidationErrorCode.InvalidWildcardUsage,
            "Invalid wildcard usage",
            "A <File Path=\"...\"> element contains a wildcard pattern, which is not supported.",
            RuleSeverity.MINOR),

        [ValidationErrorCode.XsdViolation] = Create(
            ValidationErrorCode.XsdViolation,
            "XSD schema violation",
            "The XML structure violates the .slnx schema."),

        [ValidationErrorCode.RequiredFileDoesntExistOnSystem] = Create(
            ValidationErrorCode.RequiredFileDoesntExistOnSystem,
            "Required file does not exist on the system",
            "A file required by '--required-files' does not exist on the file system."),

        [ValidationErrorCode.RequiredFileNotReferencedInSolution] = Create(
            ValidationErrorCode.RequiredFileNotReferencedInSolution,
            "Required file not referenced in solution",
            "A file required by '--required-files' exists on the file system but is not referenced as a <File> element in the solution."),
    };

    public static Rule Get(ValidationErrorCode code)
    {
        if (Rules.TryGetValue(code, out var rule))
            return rule;
        throw new ArgumentOutOfRangeException(nameof(code), code, null);
    }
}
