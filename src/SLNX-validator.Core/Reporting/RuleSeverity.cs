namespace JulianVerdurmen.SlnxValidator.Core.Reporting;

/// <summary>Universal severity level used across the entire validator pipeline.</summary>
public enum RuleSeverity
{
    /// <summary>SARIF: <c>error</c> — causes exit code 1.</summary>
    BLOCKER,
    /// <summary>SARIF: <c>error</c> — causes exit code 1.</summary>
    CRITICAL,
    /// <summary>SARIF: <c>error</c> — causes exit code 1. Default severity for most rules.</summary>
    MAJOR,
    /// <summary>SARIF: <c>warning</c> — does not cause exit code 1.</summary>
    MINOR,
    /// <summary>SARIF: <c>note</c> — does not cause exit code 1.</summary>
    INFO
}
