namespace Croicu.__project_namespace__.__package_name__.Tests.Unit;

[TestClass]
public sealed class ProgramTests
{
    [TestMethod]
    public void Start_RunsClean()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "data", "settings.json");
        var exitCode = Croicu.__project_namespace__.__package_name__.Program.Start(Array.Empty<string>(), settingsPath: settingsPath);
        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public void ParseArgs_Log_SetsLogDir()
    {
        var arguments = Croicu.__project_namespace__.__package_name__.Program.ParseArgs(["--log", "/var/log/__project_name__"]);

        Assert.AreEqual("/var/log/__project_name__", arguments.LogDir);
    }

    [TestMethod]
    public void ParseArgs_NoLog_LogDirIsNull()
    {
        var arguments = Croicu.__project_namespace__.__package_name__.Program.ParseArgs([]);

        Assert.IsNull(arguments.LogDir);
    }
}
