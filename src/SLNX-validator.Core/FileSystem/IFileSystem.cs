namespace JulianVerdurmen.SlnxValidator.Core.FileSystem;

public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    IEnumerable<string> GetFiles(string directory, string searchPattern);
    IEnumerable<string> GetDirectories(string directory);
    void CreateDirectory(string path);
    Stream CreateFile(string path);
    Stream OpenRead(string path);
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
    long GetFileSize(string path);
}
