namespace JulianVerdurmen.SlnxValidator;

internal interface IConsole
{
    Task WriteAsync(string value);
    Task WriteErrorAsync(string value);
}
