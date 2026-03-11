namespace JulianVerdurmen.SlnxValidator.Core.FileSystem;

public sealed class RealFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public IEnumerable<string> GetFiles(string directory, string searchPattern) =>
        Directory.GetFiles(directory, searchPattern);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public Stream CreateFile(string path) => File.Create(path);
}
