using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Engine;
using Legislator.Engine.Jobs;
using Legislator.TestSupport;

namespace Legislator.Engine.Tests.Audit;

/// <summary>
/// The legislated shape every audit case starts from - the ruler's `audit_repo`, which comes
/// out clean on every check, so a case plants exactly one defect and the report names exactly
/// one thing. Git is scripted rather than real: most cases are not repositories, and the two
/// that need history say so.
/// </summary>
internal static class AuditFixture
{
    public const string SkillPath = "/skill";

    public const string SkillVersion = "25";

    public const string Root = "/r";

    /// <summary>The machine's PATH in a fixture, and the one directory on it.</summary>
    public const string BinDir = "/bin";

    /// <summary>The RID and digest the fixture's installed arm answers with, matching the release record below.</summary>
    private const string ArmRid = "linux-x64";

    private const string ArmDigest = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    public static string Manifest(string version = SkillVersion, string body = "\"stacks\": [], \"keep\": [], \"ownedFiles\": []") =>
        $"{{\"legislatorVersion\": {version}, {body}}}";

    public static MockFileSystem Repo(Dictionary<string, string>? files = null, string? manifest = null)
    {
        var fs = new MockFileSystem();
        fs.AddDirectory($"{Root}/docs/ai");
        if (manifest is not null)
        {
            fs.AddFile($"{Root}/docs/ai/manifest.json", new MockFileData(manifest));
        }
        else
        {
            fs.AddFile($"{Root}/docs/ai/manifest.json", new MockFileData(Manifest()));
        }

        fs.AddFile($"{Root}/AGENTS.md", new MockFileData("# Repo\n\n@docs/okf/index.md\n"));
        fs.AddFile($"{Root}/docs/okf/index.md", new MockFileData("# OKF\n\nSee `docs/okf/codebase-map.md`.\n"));
        fs.AddFile(
            $"{Root}/docs/okf/codebase-map.md",
            new MockFileData("# Map\n\n| Directory | What |\n|---|---|\n| `src/` | code |\n| `docs/` | docs |\n"));
        fs.AddFile($"{Root}/src/a.py", new MockFileData("x = 1\n"));
        foreach (var (rel, text) in files ?? [])
        {
            fs.AddFile($"{Root}/{rel}", new MockFileData(text));
        }

        fs.AddFile($"{SkillPath}/VERSION", new MockFileData($"{SkillVersion}\n"));

        // v26 (T-13.2): check 20 asks the machine for its arm, so a fixture that carries no arm
        // is a machine with none - a Warning, correctly, and one that would make every case in
        // this file read as dirty. The clean fixture therefore installs a matching arm and ships
        // the release record that names its digest; check 20's own branches (absent, mismatched,
        // untagged) are ArmIntegrityCheckTests' subject, not every other check's background.
        fs.AddFile($"{BinDir}/{ArmName}", new MockFileData(""));
        fs.AddFile(
            $"{SkillPath}/assets/release/release.json",
            new MockFileData($"{{\"edition\": \"{SkillVersion}\", \"digests\": {{\"{ArmRid}\": \"{ArmDigest}\"}}}}"));
        return fs;
    }

    /// <summary>A tree that is no git repository - git runs and refuses, which is not git being absent.</summary>
    public static FakeProcessRunner NoRepo() => new()
    {
        OnRun = (_, _, _) => new ProcessResult(128, "", "fatal: not a git repository\n"),
    };

    /// <summary>Every git question answered with one committer date, whatever was asked.</summary>
    public static FakeProcessRunner Dated(string date) => new()
    {
        OnRun = (_, _, _) => new ProcessResult(0, $"{date}\n", ""),
    };

    public static JobResult Audit(
        IFileSystem fs, IProcessRunner? git = null, params string[] extra) =>
        new AuditJob().Run(new JobContext(
            fs, TimeProvider.System, Machine(), WithArm(git ?? NoRepo()), new LegislatorOptions(),
            Root, ["--skill", SkillPath, .. extra]));

    /// <summary>The name the arm is installed under, as check 20 resolves it.</summary>
    private const string ArmName = "legislator";

    /// <summary>A machine whose PATH holds the one directory the fixture installs the arm into.</summary>
    public static FakeEnvironment Machine()
    {
        var env = new FakeEnvironment();
        env.Vars["PATH"] = BinDir;
        return env;
    }

    /// <summary>
    /// The given process script, with `legislator version --json` answered by the installed arm.
    /// Every case's git script stays exactly what it was: the arm is a second program on the
    /// machine, not a different answer to the questions git is asked.
    /// </summary>
    public static IProcessRunner WithArm(IProcessRunner inner) => new FakeProcessRunner
    {
        OnRun = (file, args, dir) =>
            file.EndsWith(ArmName, StringComparison.Ordinal) && args.Count > 0 && args[0] == "version"
                ? new ProcessResult(0, $"{{\"version\": \"{SkillVersion}\", \"rid\": \"{ArmRid}\", \"sha256\": \"{ArmDigest}\"}}", "")
                : inner.Run(file, args, dir, TimeSpan.FromSeconds(5)),
    };

    /// <summary>The audit over a tree described as files - named apart from the overload above so a dictionary literal can never be read as a file system.</summary>
    public static JobResult Over(Dictionary<string, string>? files = null, string? manifest = null) =>
        Audit(Repo(files, manifest));

    /// <summary>The findings of one check, by the slug the report prints in front of them.</summary>
    public static IReadOnlyList<string> Findings(JobResult report, string slug) =>
        [.. report.Stdout.Split('\n').Where(line => line.StartsWith($"- [{slug}] ", StringComparison.Ordinal))];
}
