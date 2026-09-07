using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Engine.Audit;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Engine.Tests.Audit;

/// <summary>
/// The arm-integrity mechanism (R-8214, R-8215, C-12): is the deterministic arm on this machine
/// the one this edition pins, and is its binary the one the edition released? The check asks the
/// binary itself — `legislator version --json` — because a file on PATH proves nothing about what
/// runs, and compares both answers against the values recorded at tag time.
///
/// It is a MECHANISM here and not yet check 19: the audit's check set is law both arms must spell
/// identically (ruling 2026-09-02), so its slug enters `AuditChecks.Order` and `SKILL.md` § Audit
/// in T-13, together with `SKILL.md`'s own switch to the binary. The precedent is T-10's
/// `OwnedSet.CaseCollisions` — mechanism with unit tests, no slug, no twin.
/// </summary>
public sealed class ArmIntegrityCheckTests
{
    private const string Rid = "linux-x64";
    private const string Sum = "9f2c000000000000000000000000000000000000000000000000000000000000";
    private const string Edition = "26.0.0";

    /// <summary>A machine carrying the binary at <paramref name="onPath"/>, with `version --json` answering <paramref name="json"/>.</summary>
    private static (MockFileSystem Fs, FakeEnvironment Env, FakeProcessRunner Proc) Machine(string? onPath, string json)
    {
        var fs = new MockFileSystem();
        var env = new FakeEnvironment();
        if (onPath is not null)
        {
            fs.AddFile(onPath, new MockFileData(""));
            env.Vars["PATH"] = fs.Path.GetDirectoryName(onPath)!;
        }

        var proc = new FakeProcessRunner { OnRun = (_, _, _) => new ProcessResult(0, json, "") };
        return (fs, env, proc);
    }

    private static IReadOnlyList<string> Run(
        MockFileSystem fs, FakeEnvironment env, FakeProcessRunner proc, string edition = Edition) =>
        ArmIntegrityCheck.Findings(
            fs, env, proc, new LegislatorOptions(), edition,
            new Dictionary<string, string>(StringComparer.Ordinal) { [Rid] = Sum });

    [Fact]
    public void Given_the_pinned_version_and_the_released_checksum_When_audited_Then_there_is_no_finding()
    {
        var (fs, env, proc) = Machine(
            "/usr/bin/legislator",
            $$"""{"version":"{{Edition}}","rid":"{{Rid}}","sha256":"{{Sum}}"}""");

        Assert.Empty(Run(fs, env, proc));
    }

    [Fact]
    public void Given_a_binary_of_another_edition_When_audited_Then_the_finding_names_both_versions()
    {
        var (fs, env, proc) = Machine(
            "/usr/bin/legislator",
            $$"""{"version":"25.0.0","rid":"{{Rid}}","sha256":"{{Sum}}"}""");

        var finding = Assert.Single(Run(fs, env, proc));
        Assert.Contains("25.0.0", finding, StringComparison.Ordinal);
        Assert.Contains(Edition, finding, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_a_checksum_the_edition_never_released_When_audited_Then_the_finding_names_the_rid()
    {
        var (fs, env, proc) = Machine(
            "/usr/bin/legislator",
            $$"""{"version":"{{Edition}}","rid":"{{Rid}}","sha256":"{{new string('a', 64)}}"}""");

        var finding = Assert.Single(Run(fs, env, proc));
        Assert.Contains(Rid, finding, StringComparison.Ordinal);
    }

    /// <summary>R-8215's loud half: a verification job says the arm is missing rather than passing over it.</summary>
    [Fact]
    public void Given_no_binary_on_the_path_When_audited_Then_the_absence_is_the_finding()
    {
        var (fs, env, proc) = Machine(null, "");

        var finding = Assert.Single(Run(fs, env, proc));
        Assert.Contains("not on this machine", finding, StringComparison.Ordinal);
        Assert.Empty(proc.Calls);
    }

    /// <summary>A binary that answers nothing usable is an absent answer, not a silent pass.</summary>
    [Fact]
    public void Given_a_binary_whose_version_json_is_unusable_When_audited_Then_it_is_a_finding()
    {
        var (fs, env, proc) = Machine("/usr/bin/legislator", "not json at all");

        var finding = Assert.Single(Run(fs, env, proc));
        Assert.Contains("version --json", finding, StringComparison.Ordinal);
    }

    /// <summary>A rid the edition never released cannot be judged by a sum it never recorded — and saying so is the finding.</summary>
    [Fact]
    public void Given_a_rid_the_edition_did_not_release_When_audited_Then_the_finding_names_it_unreleased()
    {
        var (fs, env, proc) = Machine(
            "/usr/bin/legislator",
            $$"""{"version":"{{Edition}}","rid":"freebsd-x64","sha256":"{{Sum}}"}""");

        var finding = Assert.Single(Run(fs, env, proc));
        Assert.Contains("freebsd-x64", finding, StringComparison.Ordinal);
    }
}
