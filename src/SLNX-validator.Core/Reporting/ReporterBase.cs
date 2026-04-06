using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;

namespace JulianVerdurmen.SlnxValidator.Core.Reporting;

public abstract class ReporterBase(IFileSystem fileSystem)
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task WriteReportAsync(ReportResults results, string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            fileSystem.CreateDirectory(directory);

        await using var stream = fileSystem.CreateFile(outputPath);
        await WriteReportAsync(results, stream);
    }

    public abstract Task WriteReportAsync(ReportResults results, Stream outputStream);
}
