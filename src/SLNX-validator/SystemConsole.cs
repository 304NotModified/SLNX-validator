namespace JulianVerdurmen.SlnxValidator;

internal sealed class SystemConsole : IConsole
{
    public Task WriteAsync(string value) => Console.Out.WriteAsync(value);
    public Task WriteErrorAsync(string value) => Console.Error.WriteAsync(value);
}
