using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Legislator.Cli;
using Legislator.Core.Abstractions;
using Legislator.Engine;
using Legislator.Hooks;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Parity.Tests.Engine;

/// <summary>
/// The named twins of every `check_engine.py` assertion the audit job answers (R-8206). The
/// ruler's `audit_repo` is rebuilt here in the fake, and its git is scripted: most of its
/// fixtures are not repositories at all, so git exits non-zero and the git-backed checks stay
/// silent, exactly as they do for the ruler. The two fixtures that need history script it, and
/// the one that needs no git at all uses a runner that cannot start the executable.
/// </summary>
public sealed class AuditTwins
{
    private const string SkillPath = "/skill";

    private const string SkillVersion = "25";

    private const string Root = "/r";

    private static string Manifest(string version = SkillVersion, string keep = "\"keep\": [], ") =>
        $"{{\"legislatorVersion\": {version}, \"stacks\": [], {keep}\"ownedFiles\": []}}";

    /// <summary>The ruler's `audit_repo(files, manifest)`: a legislated shape whose every check comes out clean, plus whatever the case plants on top of it.</summary>
    private static MockFileSystem AuditRepo(Dictionary<string, string>? files = null, string? manifest = null)
    {
        var fs = new MockFileSystem();
        fs.AddDirectory($"{Root}/docs/ai");
        fs.AddFile($"{Root}/docs/ai/manifest.json", new MockFileData(manifest ?? Manifest()));
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

        // The ruler runs on a machine that has the arm installed and an edition that carries no
        // released digests yet, so check 20 answers with one Info line and no Warning. The twin
        // has to stand on the same machine or it is measuring the fixture, not the job.
        fs.AddFile($"{BinDir}/{ArmName}", new MockFileData(""));
        fs.AddFile(
            $"{SkillPath}/assets/release/release.json",
            new MockFileData($"{{\"edition\": \"{SkillVersion}\", \"digests\": {{}}}}"));
        return fs;
    }

    private const string BinDir = "/bin";

    private const string ArmName = "legislator";

    /// <summary>A machine whose PATH holds the directory the fixture installs the arm into.</summary>
    private static FakeEnvironment Machine()
    {
        var env = new FakeEnvironment();
        env.Vars["PATH"] = BinDir;
        return env;
    }

    /// <summary>The given process script, with `legislator version --json` answered by the installed arm.</summary>
    private static FakeProcessRunner WithArm(IProcessRunner inner) => new()
    {
        OnRun = (file, args, dir) =>
            file.EndsWith(ArmName, StringComparison.Ordinal) && args.Count > 0 && args[0] == "version"
                ? new ProcessResult(0, $"{{\"version\": \"{SkillVersion}\", \"rid\": \"linux-x64\", \"sha256\": \"deadbeef\"}}", "")
                : inner.Run(file, args, dir, TimeSpan.FromSeconds(5)),
    };

    /// <summary>A tree that is no git repository: git runs and says so, which is not the same as git being absent.</summary>
    private static FakeProcessRunner NoRepo() => new()
    {
        OnRun = (_, _, _) => new ProcessResult(128, "", "fatal: not a git repository\n"),
    };

    /// <summary>The ruler's PATH shim, which leaves no `git` to find at all.</summary>
    private static FakeProcessRunner NoGit() => new()
    {
        OnRun = (file, _, _) => throw ProcessStartException.For(file, new InvalidOperationException("no such file")),
    };

    private static (int Exit, string Out, string Err) Audit(
        IFileSystem fs, IProcessRunner? git = null, params string[] extra)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit = Program.Run(
            ["audit", "--skill", SkillPath, "--root", Root, .. extra], JobRegistry.Jobs, HookRegistry.Hooks, fs,
            TimeProvider.System, Machine(), WithArm(git ?? NoRepo()), TextReader.Null, stdout, stderr);

        return (exit, stdout.ToString(), stderr.ToString());
    }

    /// <summary>The ruler's planted-defect repository: one defect per check it names by slug.</summary>
    private static MockFileSystem Planted() => AuditRepo(new()
    {
        ["AGENTS.md"] = "# Repo\n\n@docs/ai/rules/core/ghost-rule.md\n@docs/okf/index.md\n",
        ["docs/okf/overview-draft.md"] = "# Draft\n\n{{PROJECT_OVERVIEW}}\n\nSee `docs/okf/index.md`.\n",
        ["docs/okf/orphan-notes.md"] = "# Orphan\n",
        [".cursorrules"] = "Always write tests first.\n",
    });

    [Fact]
    [Parity("engine", "audit_clean_repo_clean_report")]
    public void Audit_clean_repo_clean_report()
    {
        var (exit, output, _) = Audit(AuditRepo());

        // v26 (T-13.2): clean means nothing actionable, not nothing printed - check 20's
        // untagged-edition Info line stands in every audit of an edition before its tag, and
        // Info does not raise the exit code.
        Assert.Equal(0, exit);
        Assert.Contains("# AI-Layer Audit", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## Critical", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## Warning", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_untagged_edition_is_an_info_line_not_a_finding")]
    public void Audit_untagged_edition_is_an_info_line_not_a_finding()
    {
        var (exit, output, _) = Audit(AuditRepo());

        Assert.Contains("## Info", output, StringComparison.Ordinal);
        Assert.Contains("[arm-integrity]", output, StringComparison.Ordinal);
        Assert.Equal(0, exit);
    }

    [Fact]
    [Parity("engine", "audit_clean_checks_line_present")]
    public void Audit_clean_checks_line_present()
    {
        var (_, output, _) = Audit(AuditRepo());

        Assert.Contains("Clean checks:", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_report_carries_engine_stamp")]
    public void Audit_report_carries_engine_stamp()
    {
        var (_, output, _) = Audit(AuditRepo());

        Assert.Contains("legislator audit", output, StringComparison.Ordinal);
        Assert.Contains("constitution v", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_defects_exit_1")]
    public void Audit_defects_exit_1()
    {
        var (exit, _, _) = Audit(Planted());

        Assert.Equal(1, exit);
    }

    [Theory]
    [InlineData("imports-resolve", "ghost-rule.md")]
    [InlineData("unresolved-placeholders", "{{PROJECT_OVERVIEW}}")]
    [InlineData("orphan-docs", "orphan-notes.md")]
    [InlineData("foreign-structures", ".cursorrules")]
    [Parity("engine", "audit_finds_{}")]
    public void Audit_finds_the_planted_defect(string slug, string needle)
    {
        var (_, output, _) = Audit(Planted());

        Assert.Contains($"[{slug}]", output, StringComparison.Ordinal);
        Assert.Contains(needle, output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_severity_sections_present")]
    public void Audit_severity_sections_present()
    {
        var (_, output, _) = Audit(Planted());

        Assert.Contains("## Critical", output, StringComparison.Ordinal);
        Assert.Contains("## Warning", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_report_byte_stable")]
    public void Audit_report_byte_stable()
    {
        var fs = Planted();

        var (_, first, _) = Audit(fs);
        var (_, second, _) = Audit(fs);

        // Strengthened past the ruler: two runs of a job that does not exist also agree, both
        // printing nothing. Byte-stability is only a claim about a report that was printed.
        Assert.Equal(first, second);
        Assert.Contains("# AI-Layer Audit", first, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "engine_audit_writes_nothing")]
    public void Engine_audit_writes_nothing()
    {
        var fs = Planted();
        var before = fs.AllFiles.Order(StringComparer.Ordinal).ToList();

        var (exit, output, _) = Audit(fs);

        // Strengthened past the ruler: an unimplemented job also writes nothing, so the tree
        // comparison alone would be green before the port. The report has to have been printed.
        Assert.Equal(before, fs.AllFiles.Order(StringComparer.Ordinal));
        Assert.Equal(1, exit);
        Assert.Contains("# AI-Layer Audit", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "model_findings_in_pinned_sections")]
    public void Model_findings_in_pinned_sections()
    {
        var fs = Planted();
        fs.AddFile($"{Root}/mf.json", new MockFileData(
            "{\"findings\": [{\"check\": \"project-rules\", \"severity\": \"Warning\", "
            + "\"line\": \"- [project-rules] .claude/rules/x.md: contradicts core/sdd.md -> align it\"}], "
            + "\"candidates\": [\"- \\\"Always deploy on Fridays.\\\" - AGENTS.md\"]}"));

        var (_, output, _) = Audit(fs, null, "--model-findings", $"{Root}/mf.json");

        var warning = output.Split("## Warning", 2)[1].Split("##", 2)[0];
        Assert.Contains("[project-rules]", warning, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "model_candidates_appended")]
    public void Model_candidates_appended()
    {
        var fs = Planted();
        fs.AddFile($"{Root}/mf.json", new MockFileData(
            "{\"findings\": [], \"candidates\": [\"- \\\"Always deploy on Fridays.\\\" - AGENTS.md\"]}"));

        var (_, output, _) = Audit(fs, null, "--model-findings", $"{Root}/mf.json");

        Assert.Contains("## Constitution candidates", output, StringComparison.Ordinal);
        Assert.Contains("Always deploy on Fridays", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_malformed_model_findings_fails_loud")]
    public void Audit_malformed_model_findings_fails_loud()
    {
        var fs = Planted();
        fs.AddFile($"{Root}/bad.json", new MockFileData("{nope"));

        var (exit, output, stderr) = Audit(fs, null, "--model-findings", $"{Root}/bad.json");

        Assert.DoesNotContain(exit, (int[])[0, 1, 2]);
        Assert.Equal("", output);
        Assert.NotEqual("", stderr);
    }

    [Fact]
    [Parity("engine", "engine_audit_fails_loud_without_git")]
    public void Engine_audit_fails_loud_without_git()
    {
        var (exit, output, stderr) = Audit(Planted(), NoGit());

        Assert.DoesNotContain(exit, (int[])[0, 1, 2]);
        Assert.Contains("git", stderr, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("", output);
    }

    [Fact]
    [Parity("engine", "audit_constitution_header_behind")]
    public void Audit_constitution_header_behind()
    {
        var (_, output, _) = Audit(AuditRepo(manifest: Manifest("21")));

        Assert.Contains($"(skill source: v{SkillVersion}) — behind", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_finds_staleness")]
    public void Audit_finds_staleness()
    {
        var (_, output, _) = Audit(AuditRepo(manifest: Manifest("21")));

        Assert.Contains("[staleness]", output, StringComparison.Ordinal);
        Assert.Contains("legislatorVersion 21", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_keep_key_missing_info")]
    public void Audit_keep_key_missing_info()
    {
        var (_, output, _) = Audit(AuditRepo(manifest: Manifest(keep: "")));

        Assert.Contains("[keep-list]", output, StringComparison.Ordinal);
        Assert.Contains("no keep key", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_keep_path_missing")]
    public void Audit_keep_path_missing()
    {
        const string manifest = "{\"legislatorVersion\": " + SkillVersion + ", \"stacks\": [], "
            + "\"keep\": [{\"path\": \"docs/notes/gone.md\", \"reason\": \"x\"}], \"ownedFiles\": []}";

        var (_, output, _) = Audit(AuditRepo(manifest: manifest));

        Assert.Contains("[keep-list]", output, StringComparison.Ordinal);
        Assert.Contains("gone.md", output, StringComparison.Ordinal);
        Assert.Contains("missing from disk", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_map_stale_row")]
    public void Audit_map_stale_row()
    {
        var (_, output, _) = Audit(AuditRepo(new()
        {
            ["docs/okf/codebase-map.md"] = "# Map\n\n| Directory | What |\n|---|---|\n| `legacy/` | gone |\n",
        }));

        Assert.Contains("[codebase-map]", output, StringComparison.Ordinal);
        Assert.Contains("legacy/", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_map_missing_row")]
    public void Audit_map_missing_row()
    {
        var (_, output, _) = Audit(AuditRepo(new()
        {
            ["docs/okf/codebase-map.md"] = "# Map\n\n| Directory | What |\n|---|---|\n| `legacy/` | gone |\n",
        }));

        Assert.Contains("src/", output.Split("[codebase-map]", 2)[1], StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_check14_sees_backticked_names")]
    public void Audit_check14_sees_backticked_names()
    {
        var (_, output, _) = Audit(AuditRepo(new()
        {
            [".claude/rules/skills.md"] = "# Skills\n\n- **implement:** `made-up-skill-zz`\n",
        }));

        Assert.Contains("[skill-bindings]", output, StringComparison.Ordinal);
        Assert.Contains("made-up-skill-zz", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_journal_date_from_prefixed_filename")]
    public void Audit_journal_date_from_prefixed_filename()
    {
        var fs = AuditRepo(new()
        {
            ["docs/journal/2026-01-15-setup.md"] = "# 2026-01-15 — setup\n",
            ["docs/journal/README.md"] = "# j\n",
        });

        // The ruler commits the tree with GIT_COMMITTER_DATE 2026-07-01; here the same date is
        // scripted, so what is under test is the comparison, never git's own dating.
        var git = new FakeProcessRunner { OnRun = (_, _, _) => new ProcessResult(0, "2026-07-01\n", "") };

        var (_, output, _) = Audit(fs, git);

        Assert.Contains("newest entry is 2026-01-15", output, StringComparison.Ordinal);
    }

    /// <summary>
    /// Check 18, `tracker-drift`, arrived with edition v25 while this port was mid-flight and was
    /// carried as declared debt to this task (ruling 2026-09-04). It is file-local by design: the
    /// tracker itself is never read, only what `docs/backlog.md` says about its own region.
    /// </summary>
    [Fact]
    [Parity("engine", "audit_check18_item_above_marker")]
    public void Audit_check18_item_above_marker()
    {
        var (_, output, _) = Audit(AuditRepo(new()
        {
            ["docs/backlog.md"] = "# r — Backlog\n\n## BL-042 — stray item\n\n"
                + "<!-- clerk:mirror generated 2026-08-31T00:00:00Z from o/r — do not edit -->\n\n"
                + "## Ready\n\n- **BL-007 — mirrored** (#7)\n",
        }));

        Assert.Contains("[tracker-drift]", output, StringComparison.Ordinal);
        Assert.Contains("BL-042", output, StringComparison.Ordinal);
        Assert.DoesNotContain("BL-007", output.Split("[tracker-drift]", 2)[1].Split('\n')[0], StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_check18_items_while_tracker_recorded")]
    public void Audit_check18_items_while_tracker_recorded()
    {
        var (_, output, _) = Audit(AuditRepo(new()
        {
            ["docs/backlog.md"] = "# r — Backlog\n\n## BL-042 — an item\n",
            ["AGENTS.md"] = "# Repo\n\n- Task tracker: the project's sources note records it\n\n@docs/okf/index.md\n",
        }));

        Assert.Contains("[tracker-drift]", output, StringComparison.Ordinal);
        Assert.Contains("two sources of truth", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_check18_quiet_without_tracker")]
    public void Audit_check18_quiet_without_tracker()
    {
        var (_, output, _) = Audit(AuditRepo(new()
        {
            ["docs/backlog.md"] = "# r — Backlog\n\n## BL-042 — an item\n",
        }));

        Assert.DoesNotContain("[tracker-drift]", output, StringComparison.Ordinal);
    }

    /// <summary>
    /// Check 21, `waterflow-mode` (design C-6, per R-003/R-004/R-005): a waterflow declaration
    /// without a named release branch is a Warning naming the entry; with the convention, and
    /// for an explicit pair declaration, the check stays silent. This trio is the plan's one
    /// expected-red commit - the labels and their twins land before the check exists, so the
    /// without-convention half is red until T-05 greens it. The "declares nothing" half of
    /// R-005 rides the clean-audit labels (a misfiring check 21 would fail them).
    /// </summary>
    [Fact]
    [Parity("engine", "audit_check21_waterflow_without_convention")]
    public void Audit_check21_waterflow_without_convention()
    {
        var (_, output, _) = Audit(AuditRepo(new()
        {
            ["AGENTS.md"] = "# Repo\n\n- Development law mode: waterflow\n\n@docs/okf/index.md\n",
        }));

        Assert.Contains("[waterflow-mode]", output, StringComparison.Ordinal);
        Assert.Contains("AGENTS.md", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_check21_quiet_with_convention")]
    public void Audit_check21_quiet_with_convention()
    {
        var (_, output, _) = Audit(AuditRepo(new()
        {
            ["AGENTS.md"] = "# Repo\n\n- Development law mode: waterflow\n- Release branch: release/0\n\n@docs/okf/index.md\n",
        }));

        Assert.DoesNotContain("[waterflow-mode]", output, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("engine", "audit_check21_quiet_pair")]
    public void Audit_check21_quiet_pair()
    {
        var (_, output, _) = Audit(AuditRepo(new()
        {
            ["AGENTS.md"] = "# Repo\n\n- Development law mode: pair\n\n@docs/okf/index.md\n",
        }));

        Assert.DoesNotContain("[waterflow-mode]", output, StringComparison.Ordinal);
    }
}
