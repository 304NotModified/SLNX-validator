namespace JulianVerdurmen.SlnxValidator.Tests;

internal sealed class FakeConsole : IConsole
{
    public List<string> Output { get; } = [];
    public List<string> ErrorOutput { get; } = [];

    public Task WriteAsync(string value) { Output.Add(value); return Task.CompletedTask; }
    public Task WriteErrorAsync(string value) { ErrorOutput.Add(value); return Task.CompletedTask; }
}
