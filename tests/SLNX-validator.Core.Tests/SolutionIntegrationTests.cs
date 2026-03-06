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

        await Assert.That(directory).IsNotNull();

        var slnxFile = directory!.EnumerateFiles("*.slnx").First();
        var content = await File.ReadAllTextAsync(slnxFile.FullName);

        var validator = new CoreSlnxValidator(new RealFileSystem(), new XsdValidator());
        var result = await validator.ValidateAsync(content, slnxFile.DirectoryName!);

        await Assert.That(result.Errors).IsEmpty();
    }
}
