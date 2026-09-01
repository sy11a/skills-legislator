using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Legislator.Cli;
using Legislator.Core.Abstractions;
using Legislator.Engine;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Parity.Tests.Engine;

/// <summary>
/// The named twins of every `check_engine.py` assertion the detect job answers (R-8206). The
/// ruler builds a real git repository because its neighbours in that section (apply, verify)
/// need one; detect itself never asks git anything, so the twin builds the same file tree in
/// the fake and reads the same JSON out. The skill package is a fake directory too - detect
/// reads exactly one file from it, the VERSION.
/// </summary>
public sealed class DetectTwins
{
    private const string SkillPath = "/skill";

    private const string SkillVersion = "25";

    /// <summary>The ruler's `git_repo(files)` minus the git init: detect reads a tree, never a history.</summary>
    private static MockFileSystem Repo(Dictionary<string, string> files)
    {
        var fs = new MockFileSystem();
        fs.AddDirectory("/r");
        foreach (var (rel, text) in files)
        {
            fs.AddFile($"/r/{rel}", new MockFileData(text));
        }

        fs.AddFile($"{SkillPath}/VERSION", new MockFileData($"{SkillVersion}\n"));
        return fs;
    }

    /// <summary>The ruler's `eng(root, "detect", "--skill", SKILL_DIR)`, in process.</summary>
    private static (int Exit, JsonElement Json, string Raw) Detect(IFileSystem fs)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit = Program.Run(
            ["detect", "--skill", SkillPath, "--root", "/r"], JobRegistry.Jobs, fs,
            TimeProvider.System, new FakeEnvironment(), new FakeProcessRunner(), stdout, stderr);

        var raw = stdout.ToString();
        var json = exit == 0 && raw.TrimStart().StartsWith('{')
            ? JsonDocument.Parse(raw).RootElement
            : default;

        return (exit, json, raw);
    }

    private static string? Text(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object && json.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static List<string> Strings(JsonElement json, params string[] path)
    {
        var at = json;
        foreach (var step in path)
        {
            if (at.ValueKind != JsonValueKind.Object || !at.TryGetProperty(step, out at))
            {
                return [];
            }
        }

        return at.ValueKind == JsonValueKind.Array
            ? [.. at.EnumerateArray().Select(e => e.GetString() ?? "")]
            : [];
    }

    /// <summary>The ruler's `tree(root)`: every file the repository holds, so a job claiming zero writes can be held to it.</summary>
    private static List<string> Tree(MockFileSystem fs) =>
        [.. fs.AllFiles.Order(StringComparer.Ordinal)];

    [Fact]
    [Parity("engine", "detect_fresh")]
    public void Detect_fresh()
    {
        var (exit, json, raw) = Detect(Repo(new() { ["README.md"] = "hi\n" }));

        Assert.Equal(0, exit);
        Assert.Equal("fresh", Text(json, "mode"));
        Assert.Equal(JsonValueKind.Null, json.GetProperty("entry").ValueKind);
        Assert.Contains($"\"version\": \"{SkillVersion}\"", raw, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "detect_migration_agents")]
    public void Detect_migration_agents()
    {
        var (exit, json, _) = Detect(Repo(new() { ["AGENTS.md"] = "# P\n" }));

        Assert.Equal(0, exit);
        Assert.Equal("migration", Text(json, "mode"));
        Assert.Equal("AGENTS.md", Text(json, "entry"));
    }

    [Fact]
    [Parity("engine", "detect_migration_claude_and_stack_candidate")]
    public void Detect_migration_claude_and_stack_candidate()
    {
        var (exit, json, _) = Detect(Repo(new()
        {
            ["CLAUDE.md"] = "# P\n",
            ["Foo.csproj"] = "<Project/>\n",
        }));

        Assert.Equal(0, exit);
        Assert.Equal("migration", Text(json, "mode"));
        Assert.Equal("CLAUDE.md", Text(json, "entry"));
        Assert.Contains("dotnet", Strings(json, "stacks", "candidates"), StringComparer.Ordinal);
    }

    [Fact]
    [Parity("engine", "detect_upgrade_reads_legacy_profiles")]
    public void Detect_upgrade_reads_legacy_profiles()
    {
        var (exit, json, _) = Detect(Repo(new()
        {
            ["AGENTS.md"] = "# P\n",
            ["docs/ai/manifest.json"] =
                "{\"legislatorVersion\": 23, \"profiles\": [\"dotnet\"], \"ownedFiles\": [\"docs/ai/rules/core/okf.md\"]}",
        }));

        Assert.Equal(0, exit);
        Assert.Equal("upgrade", Text(json, "mode"));
        Assert.Equal(["dotnet"], Strings(json, "stacks", "subscribed"));
        Assert.Equal(["docs/ai/rules/core/okf.md"], Strings(json, "ownedFilesOld"));
        Assert.Equal(JsonValueKind.False, json.GetProperty("reconstructed").ValueKind);
    }

    [Fact]
    [Parity("engine", "detect_edge_case_reconstructs_from_disk")]
    public void Detect_edge_case_reconstructs_from_disk()
    {
        var (exit, json, _) = Detect(Repo(new()
        {
            ["AGENTS.md"] = "# P\n\n@docs/ai/rules/core/okf.md\n",
            ["docs/ai/rules/core/okf.md"] = "x\n",
            ["docs/ai/rules/stacks/dotnet/a.md"] = "x\n",
            ["opencode.json"] = "{}\n",
        }));

        Assert.Equal(0, exit);
        Assert.Equal("upgrade", Text(json, "mode"));
        Assert.Equal(JsonValueKind.True, json.GetProperty("reconstructed").ValueKind);
        Assert.Equal(["dotnet"], Strings(json, "stacks", "subscribed"));
        Assert.Equal(
            ["docs/ai/rules/core/okf.md", "docs/ai/rules/stacks/dotnet/a.md", "opencode.json"],
            Strings(json, "ownedFilesOld").Order(StringComparer.Ordinal));
    }

    [Fact]
    [Parity("engine", "detect_writes_nothing")]
    public void Detect_writes_nothing()
    {
        var fs = Repo(new()
        {
            ["AGENTS.md"] = "# P\n\n@docs/ai/rules/core/okf.md\n",
            ["docs/ai/rules/core/okf.md"] = "x\n",
            ["docs/ai/rules/stacks/dotnet/a.md"] = "x\n",
            ["opencode.json"] = "{}\n",
        });
        var before = Tree(fs);

        var (exit, _, raw) = Detect(fs);

        // Strengthened past the ruler on purpose: against an implementation that does not
        // exist the tree is trivially unchanged, so the twin would be green before the port
        // and measure nothing. It asserts the run happened as well as that it wrote nothing.
        Assert.Equal(before, Tree(fs));
        Assert.Equal(0, exit);
        Assert.NotEqual("", raw);
    }
}
