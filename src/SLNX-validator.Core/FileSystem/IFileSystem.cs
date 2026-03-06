namespace JulianVerdurmen.SlnxValidator.Core.FileSystem;

public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    string ReadAllText(string path);
    IEnumerable<string> GetFiles(string directory, string searchPattern);
}
