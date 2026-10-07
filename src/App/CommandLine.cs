namespace LogDeck.App;

/// <summary>
/// The command-line options: --tab logs|tools, --tool &lt;id&gt; to open on a tool,
/// --source &lt;id&gt; to pick one of its sources, --theme light|dark.
/// </summary>
internal static class CommandLine
{
    public static string? Value(string name)
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    /// <summary>The arguments this process was started with, to pass on when it restarts elevated.</summary>
    public static string Passthrough() => string.Join(' ',
        Environment.GetCommandLineArgs().Skip(1).Select(a => a.Contains(' ') ? $"\"{a}\"" : a));
}
