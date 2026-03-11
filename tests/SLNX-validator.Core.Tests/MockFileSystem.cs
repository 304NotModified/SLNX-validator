using JulianVerdurmen.SlnxValidator.Core.FileSystem;

namespace JulianVerdurmen.SlnxValidator.Core.Tests;

internal sealed class MockFileSystem(params string[] existingPaths) : IFileSystem
{
    private readonly HashSet<string> _existingPaths = new(existingPaths, StringComparer.OrdinalIgnoreCase);

    public List<string> CreatedDirectories { get; } = [];
    public Dictionary<string, MemoryStream> CreatedFiles { get; } = [];

    public bool FileExists(string path) => _existingPaths.Contains(path);
    public bool DirectoryExists(string path) => false;
    public IEnumerable<string> GetFiles(string directory, string searchPattern) => [];
    public void CreateDirectory(string path) => CreatedDirectories.Add(path);
    public Stream CreateFile(string path)
    {
        var stream = new MemoryStream();
        CreatedFiles[path] = stream;
        return stream;
    }
}
