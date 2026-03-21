namespace JulianVerdurmen.SlnxValidator.Core.FileSystem;

public sealed class RealFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public IEnumerable<string> GetFiles(string directory, string searchPattern) =>
        Directory.GetFiles(directory, searchPattern);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public Stream CreateFile(string path) => File.Create(path);
    public Stream OpenRead(string path) => File.OpenRead(path);
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) =>
        File.ReadAllTextAsync(path, cancellationToken);
}
