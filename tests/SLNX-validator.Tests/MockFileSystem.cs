using System.Text;
using JulianVerdurmen.SlnxValidator.Core.FileSystem;

namespace JulianVerdurmen.SlnxValidator.Tests;

internal sealed class MockFileSystem : IFileSystem
{
    private readonly HashSet<string> _existingPaths;
    private readonly Dictionary<string, string> _fileContents;
    private readonly Dictionary<string, long> _fileSizes = new(StringComparer.OrdinalIgnoreCase);

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
        return new SizeCapturingStream(ms, size => _fileSizes[path] = size);
    }
    public Stream OpenRead(string path) =>
        new MemoryStream(Encoding.UTF8.GetBytes(_fileContents.GetValueOrDefault(path, "")));
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(_fileContents.GetValueOrDefault(path, ""));
    public long GetFileSize(string path) =>
        _fileSizes.TryGetValue(path, out var size) ? size :
        CreatedFiles.TryGetValue(path, out var ms) ? ms.Length : 0;

    /// <summary>Captures the stream length before disposing the underlying <see cref="MemoryStream"/>.</summary>
    private sealed class SizeCapturingStream(MemoryStream inner, Action<long> onDispose) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            inner.WriteAsync(buffer, offset, count, cancellationToken);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.WriteAsync(buffer, cancellationToken);
        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                onDispose(inner.Length);
                inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

