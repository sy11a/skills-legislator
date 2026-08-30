using Legislator.Core.Abstractions;

namespace Legislator.Core.Tests.TestSupport;

/// <summary>Scripted <see cref="IProcessRunner"/>: every call is recorded in <see cref="Calls"/>, the result comes from <see cref="OnRun"/> (default: exit 0, empty output).</summary>
public sealed class FakeProcessRunner : IProcessRunner
{
    public Func<string, IReadOnlyList<string>, string, ProcessResult> OnRun { get; set; } = (_, _, _) => new ProcessResult(0, "", "");

    public List<(string FileName, IReadOnlyList<string> Args, string WorkingDirectory)> Calls { get; } = [];

    public ProcessResult Run(string fileName, IReadOnlyList<string> args, string workingDirectory, TimeSpan timeout)
    {
        Calls.Add((fileName, args, workingDirectory));
        return OnRun(fileName, args, workingDirectory);
    }
}
