using Croicu.__project_namespace__.Base.Sinks;
using Croicu.__project_namespace__.Mocks;

namespace Croicu.__project_namespace__.Base.Tests.Unit;

[TestClass]
public sealed class ContextTests
{
    [TestCleanup]
    public void Cleanup() => Logger.Reset();

    [TestMethod]
    public void Create_NoLogDir_DoesNotInstallFileSink()
    {
        Context.Create(new TestSettings { LogDir = null });

        // If a FileLog had been installed, this would throw (its instance guard) -- it shouldn't
        // have been.
        var logDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var sink = FileLog.Create(logDir);
        sink.Dispose();
        Directory.Delete(logDir, recursive: true);
    }

    [TestMethod]
    public void Create_SettingsLogDir_InstallsFileSinkAlongsideWhateverElseIsRegistered()
    {
        var logDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            Context.Create(new TestSettings { LogDir = logDir });

            // A FileLog is already live from Create() above -- a second one should throw.
            Assert.ThrowsExactly<InvalidOperationException>(() => FileLog.Create(logDir));
        }
        finally
        {
            Logger.Reset();
            Directory.Delete(logDir, recursive: true);
        }
    }

    [TestMethod]
    public void Create_LogDirOverride_TakesPrecedenceOverSettingsLogDir()
    {
        var settingsLogDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var overrideLogDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            Context.Create(new TestSettings { LogDir = settingsLogDir }, logDirOverride: overrideLogDir);

            Assert.IsTrue(Directory.Exists(overrideLogDir));
            Assert.IsFalse(Directory.Exists(settingsLogDir));
        }
        finally
        {
            Logger.Reset();
            if (Directory.Exists(overrideLogDir))
            {
                Directory.Delete(overrideLogDir, recursive: true);
            }

            if (Directory.Exists(settingsLogDir))
            {
                Directory.Delete(settingsLogDir, recursive: true);
            }
        }
    }

    [TestMethod]
    public void Create_DebugTrueInSettings_ResolvesDebugTrue()
    {
        var context = Context.Create(new TestSettings { Debug = true });

        Assert.IsTrue(context.Debug);
    }

    [TestMethod]
    public void Create_DebugOverrideTrue_ResolvesDebugTrueEvenWhenSettingsAreFalse()
    {
        var context = Context.Create(new TestSettings { Debug = false }, debugOverride: true);

        Assert.IsTrue(context.Debug);
    }

    [TestMethod]
    public void Create_NeitherSettingsNorOverride_ResolvesDebugFalse()
    {
        var context = Context.Create(new TestSettings { Debug = false });

        Assert.IsFalse(context.Debug);
    }

    [TestMethod]
    public void Create_NotDebug_DoesNotInstallDebugSink()
    {
        Context.Create(new TestSettings { Debug = false });

        // If a DebugLog had been installed, this would throw (its instance guard) -- it
        // shouldn't have been.
        var sink = DebugLog.Create();
        sink.Dispose();
    }

    [TestMethod]
    public void Create_Debug_InstallsDebugSinkAlongsideWhateverElseIsRegistered()
    {
        Context.Create(new TestSettings { Debug = true });

        // A DebugLog is already live from Create() above -- a second one should throw.
        Assert.ThrowsExactly<InvalidOperationException>(() => DebugLog.Create());
    }

    [TestMethod]
    public void Current_BeforeCreate_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => Context.Current);
    }

    [TestMethod]
    public void Current_AfterCreate_ReturnsTheCreatedInstance()
    {
        var context = Context.Create(new TestSettings { Debug = false });

        Assert.AreSame(context, Context.Current);
    }

    /// <summary>
    /// Proves Create() wires settings.json's log level/categories into the console sink (not just
    /// the debug sink). Create()'s internal Logger.ConfigureConsole call forces Logger's own
    /// lazily-created bootstrap ConsoleLog into existence (see Logger.cs's ActiveSinks()), which
    /// calls ConsoleLog.Create() with no writer of its own to pass -- ConsoleLog.DefaultWriterOverride
    /// lets this test observe it without redirecting the real, process-wide Console.Out, so this
    /// runs safely in parallel with everything else.
    /// </summary>
    [TestMethod]
    public void Create_AppliesSettingsLogLevelToConsoleSink()
    {
        var settings = new TestSettings { LogLevel = TelemetryLevel.Warning };
        var writer = new StringWriter();
        ConsoleLog.DefaultWriterOverride.Value = writer;
        try
        {
            Context.Create(settings);

            Logger.Info("quiet");
            Logger.Warning("audible");
        }
        finally
        {
            ConsoleLog.DefaultWriterOverride.Value = null;
        }

        var output = writer.ToString();
        Assert.DoesNotContain("quiet", output);
        Assert.Contains("audible", output);
    }

    [TestMethod]
    public void Start_RunsRunDelegateAndReturnsItsResult()
    {
        var result = Context.Start("test-app", NonExistentPath(), debugOverride: false, logDirOverride: null, () => 42);

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void Start_LogDirOverride_ThreadsThroughToCreate()
    {
        var logDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            Context.Start("test-app", NonExistentPath(), debugOverride: false, logDirOverride: logDir, () => 0);

            Assert.IsTrue(Directory.Exists(logDir));
        }
        finally
        {
            Logger.Reset();
            if (Directory.Exists(logDir))
            {
                Directory.Delete(logDir, recursive: true);
            }
        }
    }

    [TestMethod]
    public void Start_SettingsLoadFails_ReturnsOneWithoutCallingRun()
    {
        var malformedPath = WriteTempSettingsFile("not valid json");
        var runCalled = false;
        try
        {
            var result = Context.Start("test-app", malformedPath, debugOverride: false, logDirOverride: null, () =>
            {
                runCalled = true;
                return 0;
            });

            Assert.AreEqual(1, result);
            Assert.IsFalse(runCalled);
        }
        finally
        {
            File.Delete(malformedPath);
        }
    }

    [TestMethod]
    public void Start_RunThrowsAppErrorNotDebug_ReturnsOne()
    {
        var result = Context.Start("test-app", NonExistentPath(), debugOverride: false, logDirOverride: null, () => throw new AppError("boom"));

        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void Start_RunThrowsAppErrorAndDebug_Rethrows()
    {
        Assert.ThrowsExactly<AppError>(() =>
            Context.Start("test-app", NonExistentPath(), debugOverride: true, logDirOverride: null, () => throw new AppError("boom")));
    }

    private static string NonExistentPath() => Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

    private static string WriteTempSettingsFile(string content)
    {
        var path = NonExistentPath();
        File.WriteAllText(path, content);
        return path;
    }
}
