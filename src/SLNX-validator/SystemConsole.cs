namespace JulianVerdurmen.SlnxValidator;

internal sealed class SystemConsole : IConsole
{
    public IStandardStreamWriter Out { get; } = new ConsoleStreamWriter(Console.Out);
    public IStandardStreamWriter Error { get; } = new ConsoleStreamWriter(Console.Error);

    private sealed class ConsoleStreamWriter(TextWriter writer) : IStandardStreamWriter
    {
        public void Write(string value) => writer.Write(value);
    }
}
