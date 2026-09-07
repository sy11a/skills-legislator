using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace Legislator.Cli.Tests;

/// <summary>
/// `tools/publish-legislator.sh` (R-8203, C-12 as amended 2026-09-04). The contract the script
/// carries is the one the toolchain forced on it: `Cross-OS native compilation is not supported`,
/// so a loop over the edition's four RIDs cannot run on one machine. The script publishes what
/// THIS host can publish and refuses the rest by name, loudly and before doing any work — the
/// three other RIDs come from the CI matrix, which is the only place they can come from.
/// </summary>
public sealed class PublishScriptTests
{
    private static readonly bool HasBash = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    private static (int Exit, string Out, string Err) Script(params string[] args)
    {
        var info = new ProcessStartInfo("bash") { RedirectStandardOutput = true, RedirectStandardError = true };
        info.ArgumentList.Add(PublishedBinary.ScriptPath);
        foreach (var a in args)
        {
            info.ArgumentList.Add(a);
        }

        using var p = Process.Start(info)!;
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, stdout, stderr);
    }

    [Fact]
    public void Given_a_rid_of_another_operating_system_When_the_script_is_asked_for_it_Then_it_refuses_by_name()
    {
        Assert.SkipUnless(HasBash, "the script is the operator-side POSIX arm; the Windows install is Copy-Item per C-12.");

        var (exit, _, err) = Script("win-x64");

        Assert.Equal(2, exit);
        Assert.Contains("win-x64", err, StringComparison.Ordinal);
        Assert.Contains("cross-OS", err, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The refusal is cheap: it happens before the SDK is invoked, so a wrong RID costs a second, not two minutes of restore.</summary>
    [Fact]
    public void Given_a_rid_of_another_operating_system_When_the_script_refuses_Then_it_did_not_reach_the_sdk()
    {
        Assert.SkipUnless(HasBash, "POSIX arm only.");

        var clock = Stopwatch.StartNew();
        var (exit, _, err) = Script("osx-arm64");
        clock.Stop();

        // Both halves, or the assert is vacuous: an absent script also refuses instantly, and
        // timing alone cannot tell that apart from the refusal this test exists to pin.
        Assert.Equal(2, exit);
        Assert.Contains("osx-arm64", err, StringComparison.Ordinal);
        Assert.True(clock.Elapsed.TotalSeconds < 10, $"the refusal took {clock.Elapsed.TotalSeconds:F1}s — it reached the SDK.");
    }

    /// <summary>An unknown RID is not silently published as the host's: it is named and refused.</summary>
    [Fact]
    public void Given_a_rid_the_edition_does_not_release_When_the_script_is_asked_for_it_Then_it_refuses()
    {
        Assert.SkipUnless(HasBash, "POSIX arm only.");

        var (exit, _, err) = Script("plan9-x64");

        Assert.Equal(2, exit);
        Assert.Contains("plan9-x64", err, StringComparison.Ordinal);
    }

    /// <summary>The host's own RID publishes, and the run records the digest the integrity check will later ask for.</summary>
    [Fact]
    public void Given_the_hosts_own_rid_When_the_script_runs_Then_the_binary_and_its_recorded_sum_exist()
    {
        Assert.SkipUnless(HasBash, "POSIX arm only.");

        var binary = PublishedBinary.Executable.Value;
        var sums = Path.Combine(PublishedBinary.RepoRoot, "artifacts", "SHA256SUMS");

        Assert.True(File.Exists(binary), $"no binary at {binary}");
        Assert.True(File.Exists(sums), $"no digest record at {sums}");
        Assert.Contains(PublishedBinary.HostRid, File.ReadAllText(sums), StringComparison.Ordinal);
    }
}
