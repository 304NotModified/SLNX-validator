using System.Text;

namespace JulianVerdurmen.SlnxValidator.Tests;

internal sealed class TestConsole : IConsole
{
    private readonly TestStreamWriter _out = new();
    private readonly TestStreamWriter _error = new();

    public IStandardStreamWriter Out => _out;
    public IStandardStreamWriter Error => _error;

    private sealed class TestStreamWriter : IStandardStreamWriter
    {
        private readonly StringBuilder _sb = new();

        public void Write(string value) => _sb.Append(value);

        public override string ToString() => _sb.ToString();
    }
}
