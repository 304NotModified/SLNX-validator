namespace JulianVerdurmen.SlnxValidator.Core.FileSystem;

public sealed class RealFileSystem : IFileSystem
{
    public static readonly RealFileSystem Instance = new();

    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public string ReadAllText(string path) => File.ReadAllText(path);
    public IEnumerable<string> GetFiles(string directory, string searchPattern) =>
        Directory.GetFiles(directory, searchPattern);
}
