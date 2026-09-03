using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Core.Skill;
using Legislator.Engine.Jobs;
using Legislator.TestSupport;

namespace Legislator.Engine.Tests.Apply;

/// <summary>
/// The tree, the package and the job wiring the apply-family unit tests share. The parity twins
/// drive the whole pipeline; these tests reach the branches the ruler never builds a fixture
/// for, so the fixture stays small on purpose - two core rules and one stack are enough to say
/// which files a subscription selects.
/// </summary>
internal static class ApplyFixture
{
    public const string SkillPath = "/skill";

    public const string SkillVersion = "25";

    public const string Root = "/r";

    public static LegislatorOptions Options { get; } = new();

    public static RepoLayout Layout { get; } = new(Options, Root);

    public static MockFileSystem Repo(Dictionary<string, string>? files = null)
    {
        var fs = new MockFileSystem();
        fs.AddDirectory(Root);
        // The fake resolves a relative symlink target against its own current directory, where
        // the real file system does not resolve it at creation at all; pointing the fake at the
        // repository root makes the two agree for a link whose target is its own sibling, which
        // is every link the file model creates.
        fs.Directory.SetCurrentDirectory(Root);
        foreach (var (rel, text) in files ?? [])
        {
            fs.AddFile($"{Root}/{rel}", new MockFileData(text));
        }

        fs.AddFile($"{SkillPath}/VERSION", new MockFileData($"{SkillVersion}\n"));
        fs.AddFile($"{SkillPath}/assets/rules/core/okf.md", new MockFileData("# okf law\n"));
        fs.AddFile($"{SkillPath}/assets/rules/core/sdd.md", new MockFileData("# sdd law\n"));
        fs.AddFile($"{SkillPath}/assets/rules/stacks/dotnet/a.md", new MockFileData("# dotnet law\n"));
        fs.AddFile($"{SkillPath}/assets/rules/stacks/aurelia/x.md", new MockFileData("# aurelia law\n"));
        fs.AddFile($"{SkillPath}/assets/templates/opencode.json.tpl", new MockFileData("{}\n"));
        fs.AddFile($"{SkillPath}/assets/engine/engine.py", new MockFileData("# engine\n"));
        fs.AddFile(
            $"{SkillPath}/SKILL.md",
            new MockFileData("## Step 4\n\n| Target | Template |\n|---|---|\n| `docs/cases/README.md` | cases.md |\n\n## Step 5\n"));
        return fs;
    }

    public static SkillPackage Package(IFileSystem fs) => new(fs, Options, SkillPath);

    public static JobResult Run(IJob job, IFileSystem fs, IProcessRunner? proc = null, params string[] args) =>
        job.Run(new JobContext(
            fs, TimeProvider.System, new FakeEnvironment(), proc ?? new FakeProcessRunner(),
            Options, Root, args));

    /// <summary>Git that runs and refuses - the tree is no repository, so nothing apply asks it succeeds.</summary>
    public static FakeProcessRunner NoRepo() => new()
    {
        OnRun = (_, _, _) => new ProcessResult(128, "", "fatal: not a git repository\n"),
    };

    public static JobResult RunApply(IFileSystem fs, IProcessRunner? proc = null, params string[] args) =>
        Run(new ApplyJob(), fs, proc ?? NoRepo(), ["--skill", SkillPath, .. args]);

    public static JobResult RunVerify(IFileSystem fs, params string[] args) =>
        Run(new VerifyJob(), fs, null, ["--skill", SkillPath, .. args]);

    public static JobResult RunReport(IFileSystem fs, params string[] args) =>
        Run(new ReportJob(), fs, null, ["--skill", SkillPath, .. args]);
}
