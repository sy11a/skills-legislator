using System.Diagnostics;
using Legislator.Core.Abstractions;

namespace Legislator.Cli;

/// <summary>Real implementations of the Core abstractions. The host owns the statics; Core and Engine only ever see the interfaces (R-8204).</summary>
internal sealed class SystemEnvironment : IEnvironment
{
    public IEnumerable<string> VariableNames => Environment.GetEnvironmentVariables().Keys.Cast<string>();

    public string? GetVariable(string name) => Environment.GetEnvironmentVariable(name);

    public string CurrentDirectory => Environment.CurrentDirectory;

    public string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
}

/// <summary>Runs a child process with redirected output; a run that outlives its timeout is killed (whole tree) and reported as exit code -1.</summary>
internal sealed class SystemProcessRunner : IProcessRunner
{
    public ProcessResult Run(string fileName, IReadOnlyList<string> args, string workingDirectory, TimeSpan timeout)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }

        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"process failed to start: {fileName}");
        var stdout = p.StandardOutput.ReadToEndAsync();
        var stderr = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit((int)timeout.TotalMilliseconds))
        {
            p.Kill(entireProcessTree: true);
            return new ProcessResult(-1, "", "timeout");
        }

        return new ProcessResult(p.ExitCode, stdout.Result, stderr.Result);
    }
}
