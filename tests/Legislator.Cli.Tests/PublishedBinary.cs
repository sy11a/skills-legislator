using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Legislator.Cli.Tests;

/// <summary>
/// The published artifact, made once per test run and shared by every test that needs a real
/// process rather than an in-process host (C-12). Publishing is minutes; the budget test needs
/// twenty starts of the real thing, and the script's own contract needs the same artifact, so
/// the work is done once behind a <see cref="Lazy{T}"/> and the tests share the answer.
/// </summary>
internal static class PublishedBinary
{
    /// <summary>
    /// The PORTABLE RID this host can publish — the only one it can, cross-OS NativeAOT being
    /// unsupported. Deliberately not <c>RuntimeInformation.RuntimeIdentifier</c>: under the test
    /// host that answers the machine's specific RID (<c>fedora.43-x64</c> here), and a specific
    /// RID is not one an edition releases. What the edition releases is the portable four.
    /// </summary>
    public static string HostRid { get; } = $"{HostOs()}-{HostArch()}";

    private static string HostOs() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win"
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx"
        : "linux";

    private static string HostArch() =>
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";

    /// <summary>The repository root, walked up from the test binary by a file only the root holds.</summary>
    public static string RepoRoot { get; } = Find();

    public static string ScriptPath { get; } = System.IO.Path.Combine(RepoRoot, "tools", "publish-legislator.sh");

    /// <summary>The published executable, publishing it on first use.</summary>
    public static Lazy<string> Executable { get; } = new(Publish, LazyThreadSafetyMode.ExecutionAndPublication);

    private static string Find()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "evals", "check_static.py")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException($"no directory above {AppContext.BaseDirectory} holds evals/check_static.py");
    }

    private static string Publish()
    {
        if (!File.Exists(ScriptPath))
        {
            throw new InvalidOperationException(
                $"{ScriptPath} does not exist — the publish script is what makes the artifact this test measures (C-12).");
        }

        var info = new ProcessStartInfo("bash") { WorkingDirectory = RepoRoot, RedirectStandardOutput = true, RedirectStandardError = true };
        info.ArgumentList.Add(ScriptPath);
        info.ArgumentList.Add(HostRid);
        using var p = Process.Start(info)!;
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            throw new InvalidOperationException($"publish-legislator.sh {HostRid} exited {p.ExitCode}:\n{stdout}\n{stderr}");
        }

        var name = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "legislator.exe" : "legislator";
        return Path.Combine(RepoRoot, "artifacts", HostRid, name);
    }
}
