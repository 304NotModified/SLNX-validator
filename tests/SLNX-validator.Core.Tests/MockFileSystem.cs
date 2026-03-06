using JulianVerdurmen.SlnxValidator.Core.FileSystem;

namespace JulianVerdurmen.SlnxValidator.Core.Tests;

internal sealed class MockFileSystem(params string[] existingPaths) : IFileSystem
{
    private readonly HashSet<string> _existingPaths = new(existingPaths, StringComparer.OrdinalIgnoreCase);

    public bool FileExists(string path) => _existingPaths.Contains(path);
    public bool DirectoryExists(string path) => false;
    public string ReadAllText(string path) => throw new NotSupportedException();
    public IEnumerable<string> GetFiles(string directory, string searchPattern) => [];
}
