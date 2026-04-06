namespace JulianVerdurmen.SlnxValidator.Core.Reporting;

/// <summary>Universal severity level used across the entire validator pipeline.</summary>
/// <remarks>
/// Mapping to output formats:
/// <list type="table">
///   <listheader><term>Value</term><description>SARIF level / SonarQube severity</description></listheader>
///   <item><term><see cref="BLOCKER"/></term><description>SARIF: <c>error</c> / SonarQube: <c>BLOCKER</c></description></item>
///   <item><term><see cref="CRITICAL"/></term><description>SARIF: <c>error</c> / SonarQube: <c>CRITICAL</c></description></item>
///   <item><term><see cref="MAJOR"/></term><description>SARIF: <c>error</c> / SonarQube: <c>MAJOR</c></description></item>
///   <item><term><see cref="MINOR"/></term><description>SARIF: <c>warning</c> / SonarQube: <c>MINOR</c></description></item>
///   <item><term><see cref="INFO"/></term><description>SARIF: <c>note</c> / SonarQube: <c>INFO</c></description></item>
/// </list>
/// </remarks>
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
