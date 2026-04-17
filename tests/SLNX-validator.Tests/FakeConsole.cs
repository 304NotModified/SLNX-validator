namespace JulianVerdurmen.SlnxValidator.Tests;

internal sealed class FakeConsole : IConsole
{
    public List<string> OutputLines { get; } = [];
    public List<string> ErrorLines { get; } = [];

    public Task WriteLineAsync(string value) { OutputLines.Add(value); return Task.CompletedTask; }
    public Task WriteErrorLineAsync(string value) { ErrorLines.Add(value); return Task.CompletedTask; }
}
