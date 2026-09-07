using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Legislator.Core.Abstractions;
using Xunit;
using static Legislator.Parity.Tests.Engine.RunJobs;

namespace Legislator.Parity.Tests.Engine;

/// <summary>
/// The named twins of every `check_engine.py` assertion the apply job answers (R-8206). The
/// tree, the package and the record come from <see cref="RunJobs"/>, which the three run jobs
/// share; git is scripted at its interface, because the one fixture that needs a tracked file
/// is asserting that apply asked git to move it rather than moving it behind git's back.
/// </summary>
public sealed class ApplyTwins
{
    private static (int Exit, string Out, string Err) Apply(
        IFileSystem fs, IProcessRunner? git = null, params string[] extra) =>
        Run(fs, "apply", git, null, extra);

    [Fact]
    [Parity("engine", "apply_fresh_exit_0")]
    public void Apply_fresh_exit_0()
    {
        var fs = Repo(new() { ["README.md"] = "hi\n" });

        var (exit, stdout, stderr) = Apply(fs, extra: ["--stacks", "dotnet"]);

        Assert.Equal(0, exit);
        Assert.Equal("", stderr);
        Assert.StartsWith(
            $"apply: fresh mode, constitution v{SkillVersion}, stacks [\"dotnet\"]\n",
            stdout,
            StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "apply_copies_owned_set_byte_for_byte")]
    public void Apply_copies_owned_set_byte_for_byte()
    {
        var fs = Repo(new() { ["README.md"] = "hi\n" });

        Apply(fs, extra: ["--stacks", "dotnet"]);

        Assert.Equal(
            fs.File.ReadAllBytes($"{SkillPath}/assets/rules/core/okf.md"),
            fs.File.ReadAllBytes($"{Root}/docs/ai/rules/core/okf.md"));
        Assert.Equal(
            fs.File.ReadAllBytes($"{SkillPath}/assets/rules/core/sdd.md"),
            fs.File.ReadAllBytes($"{Root}/docs/ai/rules/core/sdd.md"));
        // v26 (R-8207): the package ships no engine, so nothing is copied to
        // docs/ai/engine.py - the path is absent after a fresh apply, not empty.
        Assert.False(fs.File.Exists($"{Root}/docs/ai/engine.py"));
        Assert.Equal(
            fs.File.ReadAllBytes($"{SkillPath}/assets/templates/opencode.json.tpl"),
            fs.File.ReadAllBytes($"{Root}/opencode.json"));
        Assert.True(fs.Directory.Exists($"{Root}/docs/ai/rules/stacks/dotnet"));
    }

    [Fact]
    [Parity("engine", "apply_manifest_pinned_serialization")]
    public void Apply_manifest_pinned_serialization()
    {
        var fs = Repo(new() { ["README.md"] = "hi\n" });

        Apply(fs, extra: ["--stacks", "dotnet"]);

        Assert.Equal(
            ExpectedManifest("[\"dotnet\"]", "  \"keep\": [],\n", OwnedWithDotnet),
            fs.File.ReadAllText($"{Root}/docs/ai/manifest.json"));
    }

    [Fact]
    [Parity("engine", "apply_writes_record_at_derived_tempdir_path")]
    public void Apply_writes_record_at_derived_tempdir_path()
    {
        var fs = Repo(new() { ["README.md"] = "hi\n" });

        var (_, stdout, _) = Apply(fs, extra: ["--stacks", "dotnet"]);

        Assert.True(fs.File.Exists(RecordPath(fs)));
        Assert.Contains($"run record: {RecordPath(fs)}\n", stdout, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "record_carries_mode_version_events_and_pre_snapshot")]
    public void Record_carries_mode_version_events_and_pre_snapshot()
    {
        var fs = Repo(new() { ["README.md"] = "hi\n" });

        Apply(fs, extra: ["--stacks", "dotnet"]);
        var record = Record(fs);

        Assert.Equal("fresh", record.GetProperty("mode").GetString());
        Assert.Equal(SkillVersion, record.GetProperty("version").ToString());
        Assert.Equal(["dotnet"], Strings(record, "stacks"));
        Assert.Equal(OwnedWithDotnet, Strings(record, "owned", "created"));
        Assert.Equal(JsonValueKind.Object, record.GetProperty("pre").GetProperty("step4").ValueKind);
        Assert.Equal(JsonValueKind.Array, record.GetProperty("pre").GetProperty("imports").ValueKind);
    }

    /// <summary>
    /// Strengthened past the ruler, which asserts only that no record-shaped file appears in the
    /// tree - true of a run that wrote nothing at all, and green against an empty implementation
    /// for exactly that reason. The twin adds the two halves that make the absence mean
    /// something: the run succeeded, and the record it named really is outside the repository.
    /// </summary>
    [Fact]
    [Parity("engine", "record_lives_outside_the_repo")]
    public void Record_lives_outside_the_repo()
    {
        var fs = Repo(new() { ["README.md"] = "hi\n" });

        var (exit, stdout, _) = Apply(fs, extra: ["--stacks", "dotnet"]);

        Assert.Equal(0, exit);
        Assert.NotEqual("", stdout);
        Assert.True(fs.File.Exists(RecordPath(fs)));
        Assert.DoesNotContain($"{Root}/", RecordPath(fs), StringComparison.Ordinal);
        Assert.DoesNotContain(
            Tree(fs),
            p => p.EndsWith(".json", StringComparison.Ordinal)
                && !p.EndsWith("/docs/ai/manifest.json", StringComparison.Ordinal)
                && !p.EndsWith("/opencode.json", StringComparison.Ordinal));
    }

    [Fact]
    [Parity("engine", "apply_second_run_byte_stable_and_records_unchanged")]
    public void Apply_second_run_byte_stable_and_records_unchanged()
    {
        var fs = Repo(new() { ["README.md"] = "hi\n" });
        Apply(fs, extra: ["--stacks", "dotnet"]);
        var manifest = fs.File.ReadAllText($"{Root}/docs/ai/manifest.json");

        var (exit, _, _) = Apply(fs, extra: ["--stacks", "dotnet"]);
        var record = Record(fs);

        Assert.Equal(0, exit);
        Assert.Equal(manifest, fs.File.ReadAllText($"{Root}/docs/ai/manifest.json"));
        Assert.Empty(Strings(record, "owned", "created"));
        Assert.Empty(Strings(record, "owned", "overwritten"));
        Assert.Equal(OwnedWithDotnet, Strings(record, "owned", "unchanged"));
    }

    [Fact]
    [Parity("engine", "apply_deletes_retired_and_removes_emptied_stack_dir")]
    public void Apply_deletes_retired_and_removes_emptied_stack_dir()
    {
        var fs = Repo(new()
        {
            ["AGENTS.md"] = "# P\n",
            ["docs/ai/rules/core/retired.md"] = "old\n",
            ["docs/ai/rules/stacks/aurelia/x.md"] = "old\n",
            ["docs/ai/manifest.json"] = "{\"legislatorVersion\": 23, \"stacks\": [\"dotnet\", \"aurelia\"], "
                + "\"keep\": [], \"ownedFiles\": [\"docs/ai/rules/core/retired.md\", \"docs/ai/rules/stacks/aurelia/x.md\"]}",
        });

        var (exit, _, _) = Apply(fs, extra: ["--stacks", "dotnet"]);
        var record = Record(fs);

        Assert.Equal(0, exit);
        Assert.False(fs.File.Exists($"{Root}/docs/ai/rules/core/retired.md"));
        Assert.False(fs.Directory.Exists($"{Root}/docs/ai/rules/stacks/aurelia"));
        Assert.Equal(
            ["docs/ai/rules/core/retired.md", "docs/ai/rules/stacks/aurelia/x.md"],
            Strings(record, "owned", "deleted"));
    }

    /// <summary>The ruler's keep fixture: one carried entry, one added, three refused for three different reasons, and one re-added by path.</summary>
    private static MockFileSystem KeepRepo() => Repo(new()
    {
        ["AGENTS.md"] = "# P\n",
        ["docs/notes/a.md"] = "a\n",
        ["docs/notes/b.md"] = "b\n",
        ["docs/ai/baseline.md"] = "gen\n",
        ["docs/ai/manifest.json"] = "{\"legislatorVersion\": 23, \"stacks\": [], "
            + "\"keep\": [{\"path\": \"docs/notes/b.md\", \"reason\": \"old\"}], \"ownedFiles\": []}",
    });

    private static readonly string[] KeepAdds =
    [
        "--stacks", string.Empty,
        "--keep-add", "docs/notes/a.md::hand-tuned",
        "--keep-add", "docs/ai/rules/core/okf.md::x",
        "--keep-add", "docs/nope.md::x",
        "--keep-add", "docs/ai/baseline.md::x",
        "--keep-add", "docs/notes/b.md::new reason",
    ];

    private static List<(string? Path, string? Reason)> KeepEntries(MockFileSystem fs) =>
        [.. JsonDocument.Parse(fs.File.ReadAllText($"{Root}/docs/ai/manifest.json")).RootElement
            .GetProperty("keep").EnumerateArray()
            .Select(e => (e.GetProperty("path").GetString(), e.GetProperty("reason").GetString()))];

    [Fact]
    [Parity("engine", "keep_add_and_dedupe_by_path")]
    public void Keep_add_and_dedupe_by_path()
    {
        var fs = KeepRepo();

        var (exit, _, stderr) = Apply(fs, extra: KeepAdds);

        Assert.Equal(0, exit);
        Assert.Equal("", stderr);
        Assert.Equal(
            [("docs/notes/a.md", "hand-tuned"), ("docs/notes/b.md", "new reason")],
            KeepEntries(fs));
    }

    [Fact]
    [Parity("engine", "keep_refusals_recorded_with_reasons")]
    public void Keep_refusals_recorded_with_reasons()
    {
        var fs = KeepRepo();

        Apply(fs, extra: KeepAdds);
        var refused = Record(fs).GetProperty("keep").GetProperty("refused").EnumerateArray()
            .ToDictionary(e => e.GetProperty("path").GetString()!, e => e.GetProperty("reason").GetString()!, StringComparer.Ordinal);

        Assert.Equal(
            ["docs/ai/baseline.md", "docs/ai/rules/core/okf.md", "docs/nope.md"],
            refused.Keys.Order(StringComparer.Ordinal));
        Assert.Contains("owned", refused["docs/ai/rules/core/okf.md"], StringComparison.Ordinal);
        Assert.Contains("exist", refused["docs/nope.md"], StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "keep_pinned_one_entry_per_line")]
    public void Keep_pinned_one_entry_per_line()
    {
        var fs = KeepRepo();

        Apply(fs, extra: KeepAdds);

        Assert.Contains(
            "  \"keep\": [\n    {\"path\": \"docs/notes/a.md\", \"reason\": \"hand-tuned\"},\n"
            + "    {\"path\": \"docs/notes/b.md\", \"reason\": \"new reason\"}\n  ],",
            fs.File.ReadAllText($"{Root}/docs/ai/manifest.json"),
            StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "keep_remove_only_on_request")]
    public void Keep_remove_only_on_request()
    {
        var fs = KeepRepo();
        Apply(fs, extra: KeepAdds);

        var (exit, _, _) = Apply(fs, extra: ["--stacks", string.Empty, "--keep-remove", "docs/notes/b.md"]);

        Assert.Equal(0, exit);
        Assert.Equal([("docs/notes/a.md", "hand-tuned")], KeepEntries(fs));
    }

    [Fact]
    [Parity("engine", "file_model_renames_real_claude_and_links")]
    public void File_model_renames_real_claude_and_links()
    {
        var fs = Repo(new() { ["CLAUDE.md"] = "# Real\n" });

        var (exit, _, stderr) = Apply(fs, TrackingGit(fs), "--stacks", string.Empty);

        Assert.Equal(0, exit);
        Assert.Equal("", stderr);
        Assert.True(fs.File.Exists($"{Root}/AGENTS.md"));
        Assert.Null(fs.FileInfo.New($"{Root}/AGENTS.md").LinkTarget);
        Assert.Equal("# Real\n", fs.File.ReadAllText($"{Root}/AGENTS.md"));
        Assert.Equal("AGENTS.md", fs.FileInfo.New($"{Root}/CLAUDE.md").LinkTarget);
    }

    /// <summary>
    /// The ruler asks git afterwards whether AGENTS.md is tracked; the twin asserts the same fact
    /// at git's interface - that apply asked git to move the file rather than moving it behind
    /// git's back, which is the whole content of the label.
    /// </summary>
    [Fact]
    [Parity("engine", "file_model_rename_is_git_mv_when_tracked")]
    public void File_model_rename_is_git_mv_when_tracked()
    {
        var fs = Repo(new() { ["CLAUDE.md"] = "# Real\n" });
        var git = TrackingGit(fs);

        Apply(fs, git, "--stacks", string.Empty);

        Assert.Contains(
            git.Calls,
            c => c.Args.Count == 3 && c.Args[0] == "mv" && c.Args[1] == "CLAUDE.md" && c.Args[2] == "AGENTS.md");
        Assert.True(fs.File.Exists($"{Root}/AGENTS.md"));
    }

    [Fact]
    [Parity("engine", "file_model_links_when_only_agents_exists")]
    public void File_model_links_when_only_agents_exists()
    {
        var fs = Repo(new() { ["AGENTS.md"] = "# A\n" });

        Apply(fs, extra: ["--stacks", string.Empty]);

        Assert.Equal("AGENTS.md", fs.FileInfo.New($"{Root}/CLAUDE.md").LinkTarget);
    }

    [Fact]
    [Parity("engine", "both_real_entry_documents_is_a_loud_stop_with_zero_writes")]
    public void Both_real_entry_documents_is_a_loud_stop_with_zero_writes()
    {
        var fs = Repo(new() { ["AGENTS.md"] = "# A\n", ["CLAUDE.md"] = "# C\n" });
        var before = Tree(fs);

        var (exit, _, stderr) = Apply(fs, extra: ["--stacks", string.Empty]);

        Assert.DoesNotContain(exit, (int[])[0, 1]);
        Assert.Contains("AGENTS.md", stderr, StringComparison.Ordinal);
        Assert.Contains("CLAUDE.md", stderr, StringComparison.Ordinal);
        Assert.Equal(before, Tree(fs));
    }

    /// <summary>
    /// Strengthened past the ruler, whose set difference is empty for a run that wrote nothing -
    /// green against an empty implementation. The twin pins the footprint from both sides: the
    /// run succeeded, and what appeared is exactly the owned set, the manifest and the alias.
    /// </summary>
    [Fact]
    [Parity("engine", "apply_footprint_is_exactly_the_declared_set")]
    public void Apply_footprint_is_exactly_the_declared_set()
    {
        var fs = Repo(new() { ["AGENTS.md"] = "# A\n", ["README.md"] = "r\n", ["docs/notes/a.md"] = "a\n" });
        var before = Tree(fs);

        var (exit, _, _) = Apply(fs, extra: ["--stacks", "dotnet"]);
        var appeared = Tree(fs).Except(before).Select(p => p[(Root.Length + 1)..]).Order(StringComparer.Ordinal);

        Assert.Equal(0, exit);
        Assert.Equal(
            [.. OwnedWithDotnet.Concat(["CLAUDE.md", "docs/ai/manifest.json"]).Order(StringComparer.Ordinal)],
            appeared);
    }
}
