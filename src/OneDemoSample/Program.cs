using Croicu.One.Demo.Sample.Base;

namespace Croicu.One.Demo.Sample.OneDemoSample;

public sealed record CliArguments(bool Debug = false, string? LogDir = null);

public static class Program
{
    public static int Main(string[] args) => Start(args);

    /// <summary>
    /// Testable entry point -- Main() just forwards here. settingsPath lets a test point at a
    /// fixture file instead of the real ./settings.json (see the Python template's
    /// test_main_runs_clean for the pattern this mirrors).
    /// </summary>
    internal static int Start(string[]? argv = null, string? settingsPath = null)
    {
        var arguments = ParseArgs(argv ?? Array.Empty<string>());
        return Context.Start("one-demo-sample", settingsPath, arguments.Debug, arguments.LogDir, Run);
    }

    /// <summary>
    /// The app's actual work, run once <see cref="Context.Start"/> has settings loaded and
    /// <see cref="Logger"/> wired up. Reads <see cref="Settings.Current"/>/<see cref="Context.Current"/>
    /// for whatever it needs rather than taking them as parameters.
    /// </summary>
    public static int Run()
    {
        Logger.Info("one-demo-sample: started.");
        Logger.Info("one-demo-sample: completed.");
        return 0;
    }

    /// <summary>
    /// Hand-rolled instead of a CLI-parsing package (e.g. System.CommandLine) -- the Python
    /// template has zero runtime dependencies (stdlib argparse only), and --debug is the only flag
    /// this scaffold actually needs. Reach for a real package once the CLI surface grows past what
    /// this can comfortably express.
    /// </summary>
    internal static CliArguments ParseArgs(string[] argv)
    {
        var debug = false;
        string? logDir = null;
        for (var i = 0; i < argv.Length; i++)
        {
            var arg = argv[i];
            if (arg == "--debug")
            {
                debug = true;
            }
            else if (arg == "--log")
            {
                if (i + 1 >= argv.Length)
                {
                    Logger.Error("one-demo-sample: error: --log requires a directory argument");
                    Environment.Exit(2);
                }

                logDir = argv[++i];
            }
            else if (arg is "-h" or "--help")
            {
                Logger.Print("usage: one-demo-sample [--debug] [--log <dir>]");
                Logger.Print(string.Empty);
                Logger.Print("Sample C# service code for the one-demo scanner: a few typical components (an HTTP client, a background monitor, a scheduled job) that call other services and retry on failure.");
                Logger.Print(string.Empty);
                Logger.Print("options:");
                Logger.Print("  --debug        override settings.json's debug flag");
                Logger.Print("  --log <dir>    override settings.json's logDir; write a timestamped log file into <dir>");

                Environment.Exit(0);
            }
            else
            {
                Logger.Error($"one-demo-sample: error: unrecognized argument: {arg}");
                Environment.Exit(2);
            }
        }

        return new CliArguments(Debug: debug, LogDir: logDir);
    }
}
