namespace JulianVerdurmen.SlnxValidator.Core.FileSystem;

public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    IEnumerable<string> GetFiles(string directory, string searchPattern);
    void CreateDirectory(string path);
    Stream CreateFile(string path);
}
