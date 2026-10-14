using Reqnroll;
using System.Diagnostics;

namespace Specs.Steps;

[Binding]
public sealed class LaunchSteps(ProgramRun run)
{
    [When("a person launches the program with {string}")]
    public async Task WhenAPersonLaunches(string arguments) => await Launch(arguments, withDatabase: false);

    [When("a person launches the program with {string} on the database file")]
    public async Task WhenAPersonLaunchesOnTheDatabase(string arguments) => await Launch(arguments, withDatabase: true);

    /// <summary>The built program of <c>src/App</c>, found from the repo root.</summary>
    public static string ProgramPath()
    {
        string root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "Solution.slnx")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("The repo root was not found.");
        }

        // The test output path ends in <configuration>/<framework>, as the program's does.
        var framework = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
        string path = Path.Combine(root, "src", "App", "bin", framework.Parent!.Name, framework.Name, "tracker.dll");
        return File.Exists(path) ? path : throw new FileNotFoundException("The built program was not found.", path);
    }

    private async Task Launch(string arguments, bool withDatabase)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(ProgramPath());
        foreach (string argument in arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            start.ArgumentList.Add(argument);
        }

        if (withDatabase)
        {
            start.ArgumentList.Add("--database");
            start.ArgumentList.Add(run.Database);
        }

        using Process process = Process.Start(start) ?? throw new InvalidOperationException("The program did not start.");
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        run.Output = await output;
        run.Error = await error;
        run.ExitCode = process.ExitCode;
    }
}
