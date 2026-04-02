using System.Xml.Linq;
using AwesomeAssertions;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.Validation;
using CoreSlnxValidator = JulianVerdurmen.SlnxValidator.Core.Validation.SlnxValidator;

namespace JulianVerdurmen.SlnxValidator.Core.Tests;

public class SolutionIntegrationTests
{
    [Test]
    public async Task OwnSlnxFile_HasNoValidationErrors()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !directory.EnumerateFiles("*.slnx").Any())
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull();

        var slnxFile = directory!.EnumerateFiles("*.slnx").First();
        var content = await File.ReadAllTextAsync(slnxFile.FullName);

        var validator = new CoreSlnxValidator(new RealFileSystem(), new XsdValidator(new SlnxXsdProvider()));
        var doc = XDocument.Parse(content, LoadOptions.SetLineInfo);
        var slnxFileModel = SlnxFile.FromDocument(doc, content, slnxFile.DirectoryName!);
        var result = await validator.ValidateAsync(slnxFileModel);

        result.Errors.Should().BeEmpty();
    }
}
