namespace JulianVerdurmen.SlnxValidator;

internal interface IConsole
{
    Task WriteLineAsync(string value);
    Task WriteErrorLineAsync(string value);
}
