using Croicu.__project_namespace__.Base;

namespace Croicu.__project_namespace__.__package_name__;

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
        return Context.Start("__project_name__", settingsPath, arguments.Debug, arguments.LogDir, Run);
    }

    /// <summary>
    /// The app's actual work, run once <see cref="Context.Start"/> has settings loaded and
    /// <see cref="Logger"/> wired up. Reads <see cref="Settings.Current"/>/<see cref="Context.Current"/>
    /// for whatever it needs rather than taking them as parameters.
    /// </summary>
    public static int Run()
    {
        Logger.Info("__project_name__: started.");
        Logger.Info("__project_name__: completed.");
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
                    Logger.Error("__project_name__: error: --log requires a directory argument");
                    Environment.Exit(2);
                }

                logDir = argv[++i];
            }
            else if (arg is "-h" or "--help")
            {
                Logger.Print("usage: __project_name__ [--debug] [--log <dir>]");
                Logger.Print(string.Empty);
                Logger.Print("__description__");
                Logger.Print(string.Empty);
                Logger.Print("options:");
                Logger.Print("  --debug        override settings.json's debug flag");
                Logger.Print("  --log <dir>    override settings.json's logDir; write a timestamped log file into <dir>");

                Environment.Exit(0);
            }
            else
            {
                Logger.Error($"__project_name__: error: unrecognized argument: {arg}");
                Environment.Exit(2);
            }
        }

        return new CliArguments(Debug: debug, LogDir: logDir);
    }
}
