using System.Diagnostics;
using Legislator.Core.Abstractions;

namespace Legislator.Cli;

/// <summary>Real implementations of the Core abstractions. The host owns the statics; Core and Engine only ever see the interfaces (R-8204).</summary>
public sealed class SystemEnvironment : IEnvironment
{
    public IEnumerable<string> VariableNames => Environment.GetEnvironmentVariables().Keys.Cast<string>();

    public string? GetVariable(string name) => Environment.GetEnvironmentVariable(name);

    public string CurrentDirectory => Environment.CurrentDirectory;

    public string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
}

/// <summary>Runs a child process with redirected output; a run that outlives its timeout is killed (whole tree) and reported as exit code -1.</summary>
public sealed class SystemProcessRunner : IProcessRunner
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

        Process p;
        try
        {
            p = Process.Start(psi) ?? throw new InvalidOperationException($"process failed to start: {fileName}");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // The executable is absent, or present and not runnable: the question was never
            // asked, which a job must be able to tell from a run that asked and failed (C-08).
            throw ProcessStartException.For(fileName, ex);
        }

        using var _ = p;
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
