using System.Diagnostics;
using System.Reflection;

namespace Legislator.Parity.Tests;

/// <summary>The two label sets the coverage ledger compares: what the rulers assert, and what this assembly twins.</summary>
public static class Labels
{
    /// <summary>The repository root — the directory holding <c>evals/parity_labels.py</c>, walked up from the test binary.</summary>
    public static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "evals", "parity_labels.py")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException(
            $"no directory above {AppContext.BaseDirectory} holds evals/parity_labels.py");
    }

    /// <summary>Every (ruler, label) pair the parity rulers assert, read from the instrument that parses them.</summary>
    public static HashSet<(string Ruler, string Label)> FromRulers()
    {
        var (exit, stdout, stderr) = RunInstrument();
        if (exit != 0)
        {
            throw new InvalidOperationException($"parity_labels.py exited {exit}: {stderr}");
        }

        var pairs = new HashSet<(string, string)>();
        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.TrimEnd('\r').Split('\t', 2);
            if (parts.Length != 2)
            {
                throw new InvalidOperationException($"parity_labels.py printed a line with no tab: {line}");
            }

            pairs.Add((parts[0], parts[1]));
        }

        return pairs;
    }

    /// <summary>Every (ruler, label) pair this assembly claims a twin for.</summary>
    public static HashSet<(string Ruler, string Label)> FromTwins() =>
        typeof(Labels).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes<ParityAttribute>())
            .Select(a => (a.Ruler, a.Label))
            .ToHashSet();

    static (int Exit, string Stdout, string Stderr) RunInstrument()
    {
        var script = Path.Combine(RepoRoot(), "evals", "parity_labels.py");

        // python3 everywhere the rulers run; `python` is the Windows spelling (the launcher
        // problem check_hooks.py pins per R-702).
        Exception? first = null;
        foreach (var interpreter in new[] { "python3", "python" })
        {
            try
            {
                var start = new ProcessStartInfo(interpreter, script)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using var process = Process.Start(start)
                    ?? throw new InvalidOperationException($"{interpreter} did not start");
                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                return (process.ExitCode, stdout, stderr);
            }
            catch (Exception ex)
            {
                first ??= ex;
            }
        }

        throw new InvalidOperationException(
            "no python interpreter on PATH; the parity ledger cannot be read", first);
    }
}
