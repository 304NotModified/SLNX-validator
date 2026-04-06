using JulianVerdurmen.SlnxValidator.Core.Reporting;

namespace JulianVerdurmen.SlnxValidator;

/// <summary>All options forwarded from the CLI to <see cref="ValidatorRunner.RunAsync"/>.</summary>
internal sealed class ValidatorRunnerOptions
{
    public string Input { get; }
    public string? SonarqubeReportPath { get; }
    public bool ContinueOnError { get; }
    public string? RequiredFilesPattern { get; }
    public string WorkingDirectory { get; }
    public SeverityOverrides SeverityOverrides { get; }
    public string? SarifReportPath { get; }

    public ValidatorRunnerOptions(
        string Input,
        string? SonarqubeReportPath,
        bool ContinueOnError,
        string? RequiredFilesPattern,
        string WorkingDirectory,
        SeverityOverrides? SeverityOverrides = null,
        string? SarifReportPath = null)
    {
        this.Input = Input;
        this.SonarqubeReportPath = SonarqubeReportPath;
        this.ContinueOnError = ContinueOnError;
        this.RequiredFilesPattern = RequiredFilesPattern;
        this.WorkingDirectory = WorkingDirectory;
        this.SeverityOverrides = SeverityOverrides ?? global::JulianVerdurmen.SlnxValidator.Core.Reporting.SeverityOverrides.Empty;
        this.SarifReportPath = SarifReportPath;
    }
}
