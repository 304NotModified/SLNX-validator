using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.Reporting;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Core.Tests;

public class SeverityOverridesTests
{
    #region IsFailingError – no overrides

    [Test]
    public void IsFailingError_NoOverride_MajorDefaultCode_ReturnsTrue()
    {
        // Arrange
        var overrides = SeverityOverrides.Empty;

        // Act
        var result = RuleProvider.IsFailingError(ValidationErrorCode.FileNotFound, overrides);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsFailingError_NoOverride_MinorDefaultCode_ReturnsFalse()
    {
        // Arrange
        var overrides = SeverityOverrides.Empty;

        // Act
        var result = RuleProvider.IsFailingError(ValidationErrorCode.InvalidExtension, overrides);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsFailingError_NoOverride_MinorDefaultCode_InvalidWildcardUsage_ReturnsFalse()
    {
        // Arrange
        var overrides = SeverityOverrides.Empty;

        // Act
        var result = RuleProvider.IsFailingError(ValidationErrorCode.InvalidWildcardUsage, overrides);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsFailingError – with overrides

    [Test]
    public void IsFailingError_OverriddenToMinor_ReturnsFalse()
    {
        // Arrange
        var overrides = new SeverityOverrides(new Dictionary<ValidationErrorCode, RuleSeverity?>
        {
            { ValidationErrorCode.FileNotFound, RuleSeverity.MINOR }
        });

        // Act
        var result = RuleProvider.IsFailingError(ValidationErrorCode.FileNotFound, overrides);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsFailingError_OverriddenToInfo_ReturnsFalse()
    {
        // Arrange
        var overrides = new SeverityOverrides(new Dictionary<ValidationErrorCode, RuleSeverity?>
        {
            { ValidationErrorCode.FileNotFound, RuleSeverity.INFO }
        });

        // Act
        var result = RuleProvider.IsFailingError(ValidationErrorCode.FileNotFound, overrides);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsFailingError_OverriddenToMajor_ReturnsTrue()
    {
        // Arrange
        var overrides = new SeverityOverrides(new Dictionary<ValidationErrorCode, RuleSeverity?>
        {
            { ValidationErrorCode.InvalidExtension, RuleSeverity.MAJOR }
        });

        // Act
        var result = RuleProvider.IsFailingError(ValidationErrorCode.InvalidExtension, overrides);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsFailingError_OverriddenToBlocker_ReturnsTrue()
    {
        // Arrange
        var overrides = new SeverityOverrides(new Dictionary<ValidationErrorCode, RuleSeverity?>
        {
            { ValidationErrorCode.FileNotFound, RuleSeverity.BLOCKER }
        });

        // Act
        var result = RuleProvider.IsFailingError(ValidationErrorCode.FileNotFound, overrides);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsFailingError_OverriddenToCritical_ReturnsTrue()
    {
        // Arrange
        var overrides = new SeverityOverrides(new Dictionary<ValidationErrorCode, RuleSeverity?>
        {
            { ValidationErrorCode.FileNotFound, RuleSeverity.CRITICAL }
        });

        // Act
        var result = RuleProvider.IsFailingError(ValidationErrorCode.FileNotFound, overrides);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
