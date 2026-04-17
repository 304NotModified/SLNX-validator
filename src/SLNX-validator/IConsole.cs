namespace JulianVerdurmen.SlnxValidator;

internal interface IStandardStreamWriter
{
    void Write(string value);
}

internal interface IConsole
{
    IStandardStreamWriter Out { get; }
    IStandardStreamWriter Error { get; }
}
