namespace Specs.Steps;

/// <summary>The database path and the captured result of a run, shared by the step classes of one scenario.</summary>
public sealed class ProgramRun
{
    public string Database { get; set; } = "";

    public string Output { get; set; } = "";

    public string Error { get; set; } = "";

    public int ExitCode { get; set; }
}
