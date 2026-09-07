using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Legislator.Cli;
using Legislator.Core.Abstractions;
using Legislator.Engine;
using Legislator.Hooks;
using Legislator.TestSupport;

namespace Legislator.Parity.Tests.Engine;

/// <summary>
/// The fixture the three run jobs share (R-8206). `apply`, `verify` and `report` are one
/// pipeline - the ruler runs them in that order over the same tree, reading the record the
/// first wrote - so their twins share the tree, the package and the record rather than
/// rebuilding it three times. The ruler points them at the real package; the twin builds a
/// small one in the fake, because every assertion is about what the jobs DO with a package and
/// none about which rules this edition happens to ship.
/// </summary>
internal static class RunJobs
{
    public const string SkillPath = "/skill";

    public const string SkillVersion = "25";

    public const string Root = "/r";

    /// <summary>The owned set this package delivers when `dotnet` is subscribed, ordinal-sorted as the manifest writes it.</summary>
    public static readonly string[] OwnedWithDotnet =
    [
        "docs/ai/rules/core/okf.md",
        "docs/ai/rules/core/sdd.md",
        "docs/ai/rules/stacks/dotnet/a.md",
        "opencode.json",
    ];

    /// <summary>The same set with no stack subscribed - what every fixture passing `--stacks ""` delivers.</summary>
    public static readonly string[] OwnedNoStack =
    [
        "docs/ai/rules/core/okf.md",
        "docs/ai/rules/core/sdd.md",
        "opencode.json",
    ];

    /// <summary>The two core rules this package ships, by their delivered names - what an entry document must import for the report's review section to have nothing to say.</summary>
    public static readonly string[] CoreRules = ["okf.md", "sdd.md"];

    /// <summary>
    /// The file targets of the package's Step-4 table - the scaffold snapshot verify and report
    /// read. The map and the glossary are among them because the report's review section wires
    /// exactly those two when they are newly scaffolded, and a table without them would leave
    /// that branch unreachable.
    /// </summary>
    public static readonly string[] Step4Targets =
    [
        "AGENTS.md", "CLAUDE.md", "docs/cases/README.md",
        "docs/okf/codebase-map.md", "docs/okf/glossary.md", "docs/okf/index.md",
    ];

    /// <summary>The ruler's `SKILL_DIR`, in the fake: two core rules, two stacks, the two templated files, and a Step-4 table.</summary>
    public static void AddSkill(MockFileSystem fs)
    {
        ArgumentNullException.ThrowIfNull(fs);

        fs.AddFile($"{SkillPath}/VERSION", new MockFileData($"{SkillVersion}\n"));
        fs.AddFile($"{SkillPath}/assets/rules/core/okf.md", new MockFileData("# okf law\n"));
        fs.AddFile($"{SkillPath}/assets/rules/core/sdd.md", new MockFileData("# sdd law\n"));
        fs.AddFile($"{SkillPath}/assets/rules/stacks/dotnet/a.md", new MockFileData("# dotnet law\n"));
        fs.AddFile($"{SkillPath}/assets/rules/stacks/aurelia/x.md", new MockFileData("# aurelia law\n"));
        fs.AddFile($"{SkillPath}/assets/templates/opencode.json.tpl", new MockFileData("{\"instructions\": []}\n"));
        fs.AddFile(
            $"{SkillPath}/SKILL.md",
            new MockFileData(
                "## Step 4\n\n| Target | Template |\n|---|---|\n"
                + "| `AGENTS.md` | agents.md |\n"
                + "| `CLAUDE.md` | (symlink to AGENTS.md) |\n"
                + "| `docs/cases/README.md` | cases.md |\n"
                + "| `docs/okf/codebase-map.md` | map.md |\n"
                + "| `docs/okf/glossary.md` | glossary.md |\n"
                + "| `docs/okf/index.md` | index.md |\n\n## Step 5\n"));
    }

    /// <summary>The ruler's `git_repo(files)`: the tree, plus the package the run is pointed at.</summary>
    public static MockFileSystem Repo(Dictionary<string, string> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        var fs = new MockFileSystem();
        fs.AddDirectory(Root);
        // The fake resolves a relative symlink target against its own current directory, where
        // the real file system does not resolve it at creation at all; pointing the fake at the
        // repository root makes the two agree for a link whose target is its own sibling, which
        // is every link the file model creates.
        fs.Directory.SetCurrentDirectory(Root);
        foreach (var (rel, text) in files)
        {
            fs.AddFile($"{Root}/{rel}", new MockFileData(text));
        }

        AddSkill(fs);
        return fs;
    }

    /// <summary>The ruler's `scaffold_all(root)`: every Step-4 target present, content irrelevant.</summary>
    public static void ScaffoldAll(IFileSystem fs)
    {
        ArgumentNullException.ThrowIfNull(fs);

        foreach (var target in Step4Targets)
        {
            // The entry document and its alias are the file model's business, never scaffolded
            // here - the ruler's `scaffold_all` skips them for the same reason.
            if (target is "AGENTS.md" or "CLAUDE.md")
            {
                continue;
            }

            var path = $"{Root}/{target}";
            if (!fs.File.Exists(path))
            {
                fs.Directory.CreateDirectory(fs.Path.GetDirectoryName(path)!);
                fs.File.WriteAllText(path, "# x\n");
            }
        }
    }

    /// <summary>Git that tracks whatever it is asked about and performs the move it is told to perform - the ruler's committed repository, at git's interface.</summary>
    public static FakeProcessRunner TrackingGit(MockFileSystem fs)
    {
        ArgumentNullException.ThrowIfNull(fs);

        return new()
        {
            OnRun = (_, args, _) =>
            {
                if (args.Count == 3 && args[0] == "mv")
                {
                    fs.File.Move($"{Root}/{args[1]}", $"{Root}/{args[2]}");
                    return new ProcessResult(0, "", "");
                }

                return args.Contains("ls-files")
                    ? new ProcessResult(0, $"{args[^1]}\n", "")
                    : new ProcessResult(0, "", "");
            },
        };
    }

    /// <summary>Git that runs and refuses: the tree is no repository, which is not the same as git being absent.</summary>
    public static FakeProcessRunner NoRepo() => new()
    {
        OnRun = (_, _, _) => new ProcessResult(128, "", "fatal: not a git repository\n"),
    };

    /// <summary>The day every dated report in these twins is stamped with - a value the test chose, so two runs can be compared byte for byte without racing midnight.</summary>
    public static TimeProvider Day { get; } = new FixedTimeProvider(new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero));

    /// <summary>The ruler's `eng(root, job, "--skill", SKILL_DIR, …)`, in process.</summary>
    public static (int Exit, string Out, string Err) Run(
        IFileSystem fs, string job, IProcessRunner? git = null, TimeProvider? clock = null, params string[] extra)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit = Program.Run(
            [job, "--skill", SkillPath, "--root", Root, .. extra], JobRegistry.Jobs, HookRegistry.Hooks, fs,
            clock ?? Day, new FakeEnvironment(), git ?? NoRepo(), TextReader.Null, stdout, stderr);

        return (exit, stdout.ToString(), stderr.ToString());
    }

    /// <summary>
    /// The ruler's `record_path(root)`, derived here rather than read out of the job's own
    /// output - a path the test computes independently is the only one that can prove the job
    /// derived it rather than invented it.
    /// </summary>
    public static string RecordPath(IFileSystem fs)
    {
        ArgumentNullException.ThrowIfNull(fs);

#pragma warning disable CA5350 // Not a security primitive: the digest disambiguates two repositories of the same name, and the algorithm is the Python's, which parity fixes.
        var digest = Convert.ToHexStringLower(SHA1.HashData(Encoding.UTF8.GetBytes(Root)))[..8];
#pragma warning restore CA5350
        return $"{fs.Path.GetTempPath().TrimEnd('/')}/legislator-runs/r-{digest}.json";
    }

    public static JsonElement Record(IFileSystem fs)
    {
        ArgumentNullException.ThrowIfNull(fs);

        var path = RecordPath(fs);
        return fs.File.Exists(path)
            ? JsonDocument.Parse(fs.File.ReadAllText(path)).RootElement
            : default;
    }

    /// <summary>The ruler's `tree(root)`: every FILE the repository holds, symlinks included and directories not - a directory appearing because a file was written under it is not itself a footprint.</summary>
    public static List<string> Tree(MockFileSystem fs)
    {
        ArgumentNullException.ThrowIfNull(fs);

        return [.. fs.AllFiles.Where(p => p.StartsWith(Root + "/", StringComparison.Ordinal)).Order(StringComparer.Ordinal)];
    }

    /// <summary>A string array nested under <paramref name="path"/>, ordinal-sorted, empty where the record does not carry it.</summary>
    public static List<string> Strings(JsonElement json, params string[] path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var at = json;
        foreach (var step in path)
        {
            if (at.ValueKind != JsonValueKind.Object || !at.TryGetProperty(step, out at))
            {
                return [];
            }
        }

        return at.ValueKind == JsonValueKind.Array
            ? [.. at.EnumerateArray().Select(e => e.GetString() ?? "").Order(StringComparer.Ordinal)]
            : [];
    }

    /// <summary>The manifest exactly as Step 3.7 pins it - assembled here so the twin compares against a text it wrote itself.</summary>
    public static string ExpectedManifest(string stacks, string keep, IReadOnlyList<string> owned)
    {
        ArgumentNullException.ThrowIfNull(owned);

        var text = new StringBuilder();
        text.Append("{\n");
        text.Append($"  \"legislatorVersion\": {SkillVersion},\n");
        text.Append($"  \"stacks\": {stacks},\n");
        text.Append(keep);
        text.Append("  \"ownedFiles\": [\n");
        text.Append(string.Join(",\n", owned.Select(o => $"    \"{o}\"")));
        text.Append("\n  ]\n}\n");
        return text.ToString();
    }

    /// <summary>An entry document importing every core rule this package delivers, wired the way the report's review section has nothing left to ask for.</summary>
    public static string WiredEntry() =>
        "# P\n\n"
        + string.Join('\n', CoreRules.Select(n => $"@docs/ai/rules/core/{n}"))
        + "\n@docs/okf/codebase-map.md\n\n## Boundaries\n\nnone\n\n- Domain glossary: `docs/okf/glossary.md`\n";
}
