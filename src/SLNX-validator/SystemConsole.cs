namespace JulianVerdurmen.SlnxValidator;

internal sealed class SystemConsole : IConsole
{
    public Task WriteLineAsync(string value) => Console.Out.WriteLineAsync(value);
    public Task WriteErrorLineAsync(string value) => Console.Error.WriteLineAsync(value);
}
