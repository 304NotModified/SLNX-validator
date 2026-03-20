using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace JulianVerdurmen.SlnxValidator;

internal static class RequiredFilesChecker
{
    public static async Task<int> CheckAsync(string patternsRaw, string rootDirectory)
    {
        var patterns = patternsRaw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase, preserveFilterOrder: true);

        foreach (var pattern in patterns)
        {
            if (pattern.StartsWith('!'))
                matcher.AddExclude(pattern[1..]);
            else
                matcher.AddInclude(pattern);
        }

        var directoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(rootDirectory));
        var result = matcher.Execute(directoryInfo);

        if (!result.HasMatches)
        {
            await Console.Error.WriteLineAsync($"[SLNX020] Required files check failed: no files matched the patterns: {patternsRaw}");
            return 2;
        }

        return 0;
    }
}
