namespace Croicu.One.Demo.Sample.OneDemoSample.Tests.Unit;

[TestClass]
public sealed class ProgramTests
{
    [TestMethod]
    public void Start_RunsClean()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "data", "settings.json");
        var exitCode = Croicu.One.Demo.Sample.OneDemoSample.Program.Start(Array.Empty<string>(), settingsPath: settingsPath);
        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public void ParseArgs_Log_SetsLogDir()
    {
        var arguments = Croicu.One.Demo.Sample.OneDemoSample.Program.ParseArgs(["--log", "/var/log/one-demo-sample"]);

        Assert.AreEqual("/var/log/one-demo-sample", arguments.LogDir);
    }

    [TestMethod]
    public void ParseArgs_NoLog_LogDirIsNull()
    {
        var arguments = Croicu.One.Demo.Sample.OneDemoSample.Program.ParseArgs([]);

        Assert.IsNull(arguments.LogDir);
    }
}
