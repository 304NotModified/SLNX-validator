namespace JulianVerdurmen.SlnxValidator.Core.FileSystem
{
    public interface ISlnxFileResolver
    {
        IReadOnlyList<string> Resolve(string input);
    }
}