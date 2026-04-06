using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Reporting;

public static class RuleProvider
{
    private static (ValidationErrorCode Key, Rule Rule) Create(
        ValidationErrorCode code, string name, string description,
        RuleSeverity severity = RuleSeverity.MAJOR) =>
        (code, new Rule(code.ToCode(), name, description, severity));

    private static readonly Dictionary<ValidationErrorCode, Rule> Rules =
        new (ValidationErrorCode Key, Rule Rule)[]
        {
            Create(ValidationErrorCode.FileNotFound,
                "SLNX file not found",
                "The specified .slnx file does not exist."),

            Create(ValidationErrorCode.InvalidExtension,
                "Invalid solution file extension",
                "The input file does not have a .slnx extension.",
                RuleSeverity.MINOR),

            Create(ValidationErrorCode.NotATextFile,
                "SLNX file is not a text file",
                "The file is binary and cannot be parsed as XML."),

            Create(ValidationErrorCode.InvalidXml,
                "Invalid XML",
                "The .slnx file is not valid XML."),

            Create(ValidationErrorCode.ReferencedFileNotFound,
                "Referenced file not found",
                "A file referenced in a <File Path=\"...\"> element does not exist on disk."),

            Create(ValidationErrorCode.InvalidWildcardUsage,
                "Invalid wildcard usage",
                "A <File Path=\"...\"> element contains a wildcard pattern, which is not supported.",
                RuleSeverity.MINOR),

            Create(ValidationErrorCode.XsdViolation,
                "XSD schema violation",
                "The XML structure violates the .slnx schema."),

            Create(ValidationErrorCode.RequiredFileDoesntExistOnSystem,
                "Required file does not exist on the system",
                "A file required by '--required-files' does not exist on the file system."),

            Create(ValidationErrorCode.RequiredFileNotReferencedInSolution,
                "Required file not referenced in solution",
                "A file required by '--required-files' exists on the file system but is not referenced as a <File> element in the solution."),
        }.ToDictionary(e => e.Key, e => e.Rule);

    public static Rule Get(ValidationErrorCode code)
    {
        if (Rules.TryGetValue(code, out var rule))
            return rule;
        throw new ArgumentOutOfRangeException(nameof(code), code, null);
    }

    public static ResolvedRule Resolve(ValidationErrorCode code, SeverityOverrides overrides)
    {
        var rule = Get(code);
        return new ResolvedRule(rule.Id, rule.Name, rule.Description, overrides.GetEffectiveSeverity(code));
    }
}
