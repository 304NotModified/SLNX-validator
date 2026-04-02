using JulianVerdurmen.SlnxValidator.Core.FileSystem;
using JulianVerdurmen.SlnxValidator.Core.ValidationResults;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace JulianVerdurmen.SlnxValidator.Core.Validation;

internal sealed class RequiredFilesChecker(IFileSystem fileSystem) : IRequiredFilesChecker
{
    /// <inheritdoc />
    public IReadOnlyList<string> ResolveMatchedPaths(string patternsRaw, string rootDirectory)
    {
        var patterns = patternsRaw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase, preserveFilterOrder: true);

        foreach (var pattern in patterns)
        {
            if (pattern.StartsWith('!'))
                matcher.AddExclude(pattern[1..]);
            else
                matcher.AddInclude(pattern);
        }

        var directoryInfo = new FileSystemDirectoryInfo(fileSystem, rootDirectory);
        var result = matcher.Execute(directoryInfo);

        return result.HasMatches
            ? result.Files.Select(f => Path.GetFullPath(Path.Combine(rootDirectory, f.Path))).ToList()
            : [];
    }

    /// <inheritdoc />
    public IReadOnlyList<ValidationError> CheckInSlnx(
        IReadOnlyList<string> requiredAbsolutePaths,
        SlnxFile slnxFile)
    {
        var errors = new List<ValidationError>();
        foreach (var requiredPath in requiredAbsolutePaths)
        {
            if (!slnxFile.Files.Contains(requiredPath, StringComparer.OrdinalIgnoreCase))
            {
                var relativePath = Path.GetRelativePath(slnxFile.SlnxDirectory, requiredPath).Replace('\\', '/');
                errors.Add(new ValidationError(
                    ValidationErrorCode.RequiredFileNotReferencedInSolution,
                    $"Required file is not referenced in the solution: {requiredPath}" +
                    $" — add: <File Path=\"{relativePath}\" />"));
            }
        }

        return errors;
    }

    private sealed class FileSystemDirectoryInfo(IFileSystem fileSystem, string path) : DirectoryInfoBase
    {
        public override string Name => Path.GetFileName(path) ?? path;
        public override string FullName => path;
        public override DirectoryInfoBase? ParentDirectory => null;

        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
        {
            if (!fileSystem.DirectoryExists(path))
                return [];

            var files = fileSystem.GetFiles(path, "*")
                .Select(f => (FileSystemInfoBase)new FileSystemFileInfo(Path.GetFileName(f), f, this));

            var dirs = fileSystem.GetDirectories(path)
                .Select(d => (FileSystemInfoBase)new FileSystemDirectoryInfo(fileSystem, d));

            return files.Concat(dirs);
        }

        public override DirectoryInfoBase? GetDirectory(string name) =>
            new FileSystemDirectoryInfo(fileSystem, Path.Combine(path, name));

        public override FileInfoBase? GetFile(string name) =>
            new FileSystemFileInfo(name, Path.Combine(path, name), this);
    }

    private sealed class FileSystemFileInfo(string name, string fullName, DirectoryInfoBase? parentDirectory) : FileInfoBase
    {
        public override string Name { get; } = name;
        public override string FullName { get; } = fullName;
        public override DirectoryInfoBase? ParentDirectory { get; } = parentDirectory;
    }
}

