using System.Text;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;

namespace JulianVerdurmen.SlnxValidator.Tests;

internal sealed class MockFileSystem : IFileSystem
{
    private readonly HashSet<string> _existingPaths;
    private readonly Dictionary<string, string> _fileContents;

    /// <summary>Create a mock with files that exist but have no specific content.</summary>
    public MockFileSystem(params string[] existingPaths)
    {
        _existingPaths = new(existingPaths, StringComparer.OrdinalIgnoreCase);
        _fileContents = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Create a mock where each entry represents a file path → content mapping.</summary>
    public MockFileSystem(Dictionary<string, string> fileContents)
    {
        _fileContents = new(fileContents, StringComparer.OrdinalIgnoreCase);
        _existingPaths = new(_fileContents.Keys, StringComparer.OrdinalIgnoreCase);
    }

    public List<string> CreatedDirectories { get; } = [];
    public Dictionary<string, MemoryStream> CreatedFiles { get; } = [];

    public bool FileExists(string path) => _existingPaths.Contains(path);
    public bool DirectoryExists(string path) => false;
    public IEnumerable<string> GetFiles(string directory, string searchPattern) => [];
    public IEnumerable<string> GetDirectories(string directory) => [];
    public void CreateDirectory(string path) => CreatedDirectories.Add(path);
    public Stream CreateFile(string path)
    {
        var ms = new MemoryStream();
        CreatedFiles[path] = ms;
        return ms;
    }
    public Stream OpenRead(string path) =>
        new MemoryStream(Encoding.UTF8.GetBytes(_fileContents.GetValueOrDefault(path, "")));
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(_fileContents.GetValueOrDefault(path, ""));
    public long GetFileSize(string path) => 0;
}

