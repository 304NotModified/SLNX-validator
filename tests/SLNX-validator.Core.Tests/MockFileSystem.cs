using JulianVerdurmen.SlnxValidator.Core.FileSystem;

namespace JulianVerdurmen.SlnxValidator.Core.Tests;

internal sealed class MockFileSystem(params string[] existingPaths) : IFileSystem
{
    private readonly HashSet<string> _existingPaths = new(existingPaths, StringComparer.OrdinalIgnoreCase);

    public bool FileExists(string path) => _existingPaths.Contains(path);
    public bool DirectoryExists(string path) => false;
    public IEnumerable<string> GetFiles(string directory, string searchPattern) => [];
    public void CreateDirectory(string path) { }
    public Stream CreateFile(string path) => new MemoryStream();
}
