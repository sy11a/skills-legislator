using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;

namespace Legislator.Engine.Audit;

/// <summary>
/// Is the deterministic arm on this machine the one this edition pins, and is its binary the one
/// the edition released (R-8214, R-8215, C-12)?
///
/// The check asks the BINARY, not the file system: a name on PATH proves nothing about what runs,
/// so it drives `legislator version --json` and judges the three facts that come back. Absence is
/// a finding rather than a silent pass - R-8215's loud half, the hooks being the ones that fail
/// open, never a verification job.
///
/// It is a mechanism and not yet a numbered audit check: the audit's check set is law both arms
/// must spell identically (ruling 2026-09-02), so the slug and its article in `SKILL.md` § Audit
/// arrive in T-13 with the law's own switch to the binary. The edition and the released digests
/// are parameters for the same reason - where the tag-time record lives is that task's call, and
/// a fleet member has no `evals/benchmarks/` of its own to read.
/// </summary>
public static class ArmIntegrityCheck
{
    /// <summary>The name the arm is installed under - the one the hooks resolve through PATH.</summary>
    private const string Arm = "legislator";

    /// <summary>What the running arm contradicts about the edition, one line per contradiction; empty when it matches.</summary>
    public static IReadOnlyList<string> Findings(
        IFileSystem fs,
        IEnvironment env,
        IProcessRunner proc,
        LegislatorOptions options,
        string edition,
        IReadOnlyDictionary<string, string> released)
    {
        ArgumentNullException.ThrowIfNull(proc);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(released);

        var binary = ExecutableLookup.Which(fs, env, options, Arm);
        if (binary is null)
        {
            return [$"`{Arm}` is not on this machine — the deterministic arm this edition pins ({edition}) cannot be verified; install it with tools/install-legislator.sh"];
        }

        var answer = proc.Run(binary, ["version", "--json"], env.CurrentDirectory,
                              TimeSpan.FromSeconds(options.GitTimeoutSeconds.Value));
        if (!TryRead(answer.Stdout, out var version, out var rid, out var digest))
        {
            return [$"`{Arm} version --json` at {binary} answered nothing usable — the arm cannot report its own identity"];
        }

        var findings = new List<string>();
        if (!string.Equals(version, edition, StringComparison.Ordinal))
        {
            findings.Add($"the installed arm reports {version} where this edition pins {edition} — reinstall from the edition's release");
        }

        if (released.Count == 0)
        {
            // An edition that has not been tagged has released nothing, so there is no digest to
            // contradict. Saying so is the wiring's job (an Info line, per `artifact-lifecycle`'s
            // no-silent-caps rule); inventing a mismatch here would make every audit of an
            // in-flight edition report a fault that does not exist.
        }
        else if (!released.TryGetValue(rid, out var expected))
        {
            findings.Add($"the installed arm was built for {rid}, which this edition never released — its digest cannot be checked against anything");
        }
        else if (!string.Equals(digest, expected, StringComparison.OrdinalIgnoreCase))
        {
            findings.Add($"the {rid} arm's digest is {Short(digest)} where the release recorded {Short(expected)} — the binary is not the one this edition published");
        }

        return findings;
    }

    /// <summary>The three facts `version --json` carries, or false when the answer is not that shape.</summary>
    private static bool TryRead(string stdout, out string version, out string rid, out string digest)
    {
        version = rid = digest = "";
        try
        {
            using var doc = JsonDocument.Parse(stdout);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            version = Text(doc.RootElement, "version");
            rid = Text(doc.RootElement, "rid");
            digest = Text(doc.RootElement, "sha256");
            return version.Length > 0 && rid.Length > 0 && digest.Length > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string Text(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";

    /// <summary>Enough of a digest to compare by eye, without a line of hex nobody reads.</summary>
    private static string Short(string digest) =>
        digest.Length <= 12 ? digest : string.Concat(digest.AsSpan(0, 12), "…");
}
