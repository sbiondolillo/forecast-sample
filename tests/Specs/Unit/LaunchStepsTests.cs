using Specs.Steps;
using Xunit;

namespace Specs.Unit;

public sealed class LaunchStepsTests
{
    [Fact]
    public void ProgramPathNamesTheBuiltProgram() =>
        Assert.EndsWith("tracker.dll", LaunchSteps.ProgramPath(), StringComparison.Ordinal);

    [Fact]
    public void ProgramRunStartsEmpty()
    {
        var run = new ProgramRun();
        Assert.Equal("", run.Output);
        Assert.Equal("", run.Error);
        Assert.Equal(0, run.ExitCode);
    }
}
