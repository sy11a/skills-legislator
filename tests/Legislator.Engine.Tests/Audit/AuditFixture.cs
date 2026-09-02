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

        fs.AddFile($"{Root}/docs/ai/engine.py", new MockFileData("# engine\n"));
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
            fs, TimeProvider.System, new FakeEnvironment(), git ?? NoRepo(), new LegislatorOptions(),
            Root, ["--skill", SkillPath, .. extra]));

    /// <summary>The audit over a tree described as files - named apart from the overload above so a dictionary literal can never be read as a file system.</summary>
    public static JobResult Over(Dictionary<string, string>? files = null, string? manifest = null) =>
        Audit(Repo(files, manifest));

    /// <summary>The findings of one check, by the slug the report prints in front of them.</summary>
    public static IReadOnlyList<string> Findings(JobResult report, string slug) =>
        [.. report.Stdout.Split('\n').Where(line => line.StartsWith($"- [{slug}] ", StringComparison.Ordinal))];
}
