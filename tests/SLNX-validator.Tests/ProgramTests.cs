using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.SonarQubeReporting;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;

namespace JulianVerdurmen.SlnxValidator.Tests;

public class ProgramTests
{
    #region ParseSeverityOverrides – basic parsing

    [Test]
    public void ParseSeverityOverrides_NoOverrides_ReturnsEmptyDictionary()
    {
        // Arrange / Act
        var result = Program.ParseSeverityOverrides(null, null, null, null, null, null);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void ParseSeverityOverrides_SingleCode_ParsesCorrectly()
    {
        // Arrange / Act
        var result = Program.ParseSeverityOverrides(null, null, null, "SLNX011", null, null);

        // Assert
        result.Should().HaveCount(1);
        result[ValidationErrorCode.ReferencedFileNotFound].Should().Be(SonarRuleSeverity.MINOR);
    }

    [Test]
    public void ParseSeverityOverrides_CommaSeparatedCodes_ParsesBoth()
    {
        // Arrange / Act
        var result = Program.ParseSeverityOverrides(null, null, null, "SLNX011,SLNX012", null, null);

        // Assert
        result[ValidationErrorCode.ReferencedFileNotFound].Should().Be(SonarRuleSeverity.MINOR);
        result[ValidationErrorCode.InvalidWildcardUsage].Should().Be(SonarRuleSeverity.MINOR);
    }

    [Test]
    public void ParseSeverityOverrides_EnumNameCode_ParsesCorrectly()
    {
        // Arrange / Act
        var result = Program.ParseSeverityOverrides(null, null, null, "ReferencedFileNotFound", null, null);

        // Assert
        result[ValidationErrorCode.ReferencedFileNotFound].Should().Be(SonarRuleSeverity.MINOR);
    }

    [Test]
    public void ParseSeverityOverrides_IgnoreCode_SetsToNull()
    {
        // Arrange / Act
        var result = Program.ParseSeverityOverrides(null, null, null, null, null, "SLNX011");

        // Assert
        result[ValidationErrorCode.ReferencedFileNotFound].Should().BeNull();
    }

    [Test]
    public void ParseSeverityOverrides_UnknownCode_ThrowsInvalidOperationException()
    {
        // Arrange / Act
        var act = () => Program.ParseSeverityOverrides(null, null, null, "SLNX999", null, null);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*SLNX999*");
    }

    #endregion

    #region ParseSeverityOverrides – wildcard expansion

    [Test]
    public void ParseSeverityOverrides_Wildcard_ExpandsToAllCodes()
    {
        // Arrange / Act
        var result = Program.ParseSeverityOverrides(null, null, null, null, "*", null);

        // Assert
        var allCodes = Enum.GetValues<ValidationErrorCode>();
        result.Should().HaveCount(allCodes.Length);
        result.Should().AllSatisfy(kvp => kvp.Value.Should().Be(SonarRuleSeverity.INFO));
    }

    [Test]
    public void ParseSeverityOverrides_IgnoreWildcard_SetsAllToNull()
    {
        // Arrange / Act
        var result = Program.ParseSeverityOverrides(null, null, null, null, null, "*");

        // Assert
        var allCodes = Enum.GetValues<ValidationErrorCode>();
        result.Should().HaveCount(allCodes.Length);
        result.Should().AllSatisfy(kvp => kvp.Value.Should().BeNull());
    }

    #endregion

    #region ParseSeverityOverrides – specific codes beat wildcards

    [Test]
    public void ParseSeverityOverrides_SpecificCodeOverridesWildcard_InfoAllMajorSLNX011()
    {
        // Arrange / Act: --info * --major SLNX011
        var result = Program.ParseSeverityOverrides(null, null, "SLNX011", null, "*", null);

        // Assert: SLNX011 (ReferencedFileNotFound) should be MAJOR, everything else INFO
        var allCodes = Enum.GetValues<ValidationErrorCode>();
        foreach (var code in allCodes)
        {
            if (code == ValidationErrorCode.ReferencedFileNotFound)
                result[code].Should().Be(SonarRuleSeverity.MAJOR);
            else
                result[code].Should().Be(SonarRuleSeverity.INFO);
        }
    }

    [Test]
    public void ParseSeverityOverrides_IgnoreAllMajorSpecificCode_SpecificCodeWins()
    {
        // Arrange / Act: --ignore * --major SLNX013
        var result = Program.ParseSeverityOverrides(null, null, "SLNX013", null, null, "*");

        // Assert: SLNX013 (XsdViolation) should be MAJOR; all others should be null (ignored)
        result[ValidationErrorCode.XsdViolation].Should().Be(SonarRuleSeverity.MAJOR);
        result[ValidationErrorCode.ReferencedFileNotFound].Should().BeNull();
        result[ValidationErrorCode.FileNotFound].Should().BeNull();
    }

    [Test]
    public void ParseSeverityOverrides_MinorAllInfoSpecificCode_SpecificCodeWins()
    {
        // Arrange / Act: --minor * --info SLNX001
        var result = Program.ParseSeverityOverrides(null, null, null, "*", "SLNX001", null);

        // Assert: SLNX001 (FileNotFound) should be INFO; all others should be MINOR
        result[ValidationErrorCode.FileNotFound].Should().Be(SonarRuleSeverity.INFO);
        result[ValidationErrorCode.XsdViolation].Should().Be(SonarRuleSeverity.MINOR);
    }

    #endregion
}
