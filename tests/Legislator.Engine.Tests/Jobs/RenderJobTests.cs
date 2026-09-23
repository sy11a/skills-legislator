using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Engine.Jobs;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Engine.Tests.Jobs;

/// <summary>
/// The render job, fragment by fragment: reads docs/changes/*.md and writes the three views
/// — CHANGELOG.md, docs/okf/log.md, docs/journal/YYYY-MM-DD.md — idempotently by case key.
/// </summary>
public sealed class RenderJobTests
{
    private static MockFileSystem Repo(Dictionary<string, string> files)
    {
        var fs = new MockFileSystem();
        fs.AddDirectory("/r/docs/changes");
        fs.AddDirectory("/r/docs/okf");
        fs.AddDirectory("/r/docs/journal");
        foreach (var (rel, text) in files)
        {
            fs.AddFile($"/r/{rel}", new MockFileData(text));
        }

        return fs;
    }

    private static JobResult Run(IFileSystem fs, LegislatorOptions? options = null) =>
        new RenderJob().Run(new JobContext(
            fs, TimeProvider.System, new FakeEnvironment(), new FakeProcessRunner(),
            options ?? new LegislatorOptions(), "/r", []));

    private static string Fragment(string caseKey, string kind, string date,
        string changelog = "- Entry.\n", string okfLog = "Okf entry.\n", string journal = "Journal entry.\n") =>
        $"---\ncase: {caseKey}\nissue: 1\nkind: {kind}\ndate: {date}\n---\n\n## changelog\n\n{changelog}\n## okf-log\n\n{okfLog}\n## journal\n\n{journal}\n";

    private static string ChangelogSkeleton =>
        "# Changelog\n\nAll notable changes to this project are documented here.\n\n" +
        "The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).\n\n" +
        "## [Unreleased]\n\n### Added\n\n### Changed\n\n### Fixed\n\n### Removed\n\n";

    [Fact]
    public void No_changes_directory_is_clean()
    {
        var fs = Repo(new Dictionary<string, string>());
        var result = Run(fs);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void Empty_changes_directory_is_clean()
    {
        var fs = Repo(new Dictionary<string, string>());
        fs.AddDirectory("/r/docs/changes");
        var result = Run(fs);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void A_fragment_without_front_matter_is_refused()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["docs/changes/BL-001.md"] = "## changelog\n\n- Entry.\n\n## okf-log\n\nOkf.\n\n## journal\n\nJournal.\n",
        });

        var result = Run(fs);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("malformed fragment — no YAML front matter", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void A_fragment_with_missing_case_is_refused()
    {
        // BL-410: the fixture was `no-case.md`, which the name gate now reads as
        // furniture rather than a fragment — correctly, and the suite caught it. The
        // test's subject is front matter with no `case` key, not the file's name, so
        // the file is named for a case and the front matter is the thing left short.
        var fs = Repo(new Dictionary<string, string>
        {
            ["docs/changes/BL-002.md"] = "---\nissue: 1\nkind: Added\ndate: 2026-09-16\n---\n\n## changelog\n\n- Entry.\n\n## okf-log\n\nOkf.\n\n## journal\n\nJournal.\n",
        });

        var result = Run(fs);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("missing 'case'", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void A_fragment_with_missing_issue_is_refused()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["docs/changes/BL-001.md"] = "---\ncase: BL-001\nkind: Added\ndate: 2026-09-16\n---\n\n## changelog\n\n- Entry.\n\n## okf-log\n\nOkf.\n\n## journal\n\nJournal.\n",
        });

        var result = Run(fs);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("missing 'issue'", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void A_fragment_with_invalid_kind_is_refused()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["docs/changes/BL-001.md"] = "---\ncase: BL-001\nissue: 1\nkind: Unknown\ndate: 2026-09-16\n---\n\n## changelog\n\n- Entry.\n\n## okf-log\n\nOkf.\n\n## journal\n\nJournal.\n",
        });

        var result = Run(fs);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("kind' must be Added, Changed, Fixed or Removed", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void A_fragment_with_missing_date_is_refused()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["docs/changes/BL-001.md"] = "---\ncase: BL-001\nissue: 1\nkind: Added\n---\n\n## changelog\n\n- Entry.\n\n## okf-log\n\nOkf.\n\n## journal\n\nJournal.\n",
        });

        var result = Run(fs);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("missing or unparseable 'date'", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void A_fragment_with_missing_section_is_refused()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["docs/changes/BL-001.md"] = "---\ncase: BL-001\nissue: 1\nkind: Added\ndate: 2026-09-16\n---\n\n## changelog\n\n- Entry.\n",
        });

        var result = Run(fs);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("missing ## changelog or ## journal section", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void A_fragment_without_an_okf_log_section_renders()
    {
        // BL-410, from the round: `core/changelog.md` has made `## okf-log` optional and
        // normally absent since BL-347, and render demanded all three anyway — refusing
        // every fragment written to the law of its own edition. 25 of Architector's 27
        // and 6 of this repo's 7 carry no such section. Unreported because render has
        // never run in the fleet: no `<!-- rendered:` marker exists in any repo.
        var fs = Repo(new Dictionary<string, string>
        {
            ["docs/changes/BL-001.md"] = "---\ncase: BL-001\nissue: 1\nkind: Added\ndate: 2026-09-16\n---\n\n## changelog\n\n- Entry.\n\n## journal\n\nNothing owed.\n",
        });

        var result = Run(fs);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void The_homes_own_readme_is_not_rendered_as_a_fragment()
    {
        // Render reads the same directory as the lint and must draw the same line:
        // refusing the scaffolded README here stopped the render of every lawful
        // fragment beside it.
        var fs = Repo(new Dictionary<string, string>
        {
            ["docs/changes/README.md"] = "# Change Fragments\n\nOne fragment per case.\n",
            ["docs/changes/BL-001.md"] = "---\ncase: BL-001\nissue: 1\nkind: Added\ndate: 2026-09-16\n---\n\n## changelog\n\n- Entry.\n\n## journal\n\nNothing owed.\n",
        });

        var result = Run(fs);

        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("README.md", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void A_valid_fragment_renders_into_the_changelog()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["CHANGELOG.md"] = ChangelogSkeleton,
            ["docs/changes/BL-001.md"] = Fragment("BL-001", "Added", "2026-09-16",
                "- Added change fragments.\n"),
        });

        var result = Run(fs);

        Assert.Equal(0, result.ExitCode);
        var changelog = fs.File.ReadAllText("/r/CHANGELOG.md");
        Assert.Contains("<!-- rendered: BL-001 -->", changelog, StringComparison.Ordinal);
        Assert.Contains("- Added change fragments.", changelog, StringComparison.Ordinal);
    }

    [Fact]
    public void A_valid_fragment_renders_into_the_okf_log()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["docs/changes/BL-001.md"] = Fragment("BL-001", "Added", "2026-09-16",
                okfLog: "Added ChangeFragment concept.\n"),
        });

        var result = Run(fs);

        Assert.Equal(0, result.ExitCode);
        var log = fs.File.ReadAllText("/r/docs/okf/log.md");
        Assert.Contains("<!-- rendered: BL-001 -->", log, StringComparison.Ordinal);
        Assert.Contains("Added ChangeFragment concept.", log, StringComparison.Ordinal);
    }

    [Fact]
    public void A_valid_fragment_renders_into_the_journal()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["docs/changes/BL-001.md"] = Fragment("BL-001", "Added", "2026-09-16",
                journal: "Implemented change fragments.\n"),
        });

        var result = Run(fs);

        Assert.Equal(0, result.ExitCode);
        var journal = fs.File.ReadAllText("/r/docs/journal/2026-09-16.md");
        Assert.Contains("<!-- rendered: BL-001 -->", journal, StringComparison.Ordinal);
        Assert.Contains("Implemented change fragments.", journal, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_fragments_sort_by_date_then_case()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["CHANGELOG.md"] = ChangelogSkeleton,
            ["docs/changes/BL-002.md"] = Fragment("BL-002", "Added", "2026-09-17",
                "- Second entry.\n"),
            ["docs/changes/BL-001.md"] = Fragment("BL-001", "Added", "2026-09-16",
                "- First entry.\n"),
        });

        var result = Run(fs);

        Assert.Equal(0, result.ExitCode);
        var changelog = fs.File.ReadAllText("/r/CHANGELOG.md");
        var first = changelog.IndexOf("- First entry.", StringComparison.Ordinal);
        var second = changelog.IndexOf("- Second entry.", StringComparison.Ordinal);
        Assert.True(first >= 0);
        Assert.True(second > first);
    }

    [Fact]
    public void Fragments_group_by_kind_in_changelog()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["CHANGELOG.md"] = ChangelogSkeleton,
            ["docs/changes/BL-001.md"] = Fragment("BL-001", "Fixed", "2026-09-16",
                "- Fixed a bug.\n"),
            ["docs/changes/BL-002.md"] = Fragment("BL-002", "Added", "2026-09-17",
                "- Added a feature.\n"),
        });

        var result = Run(fs);

        Assert.Equal(0, result.ExitCode);
        var changelog = fs.File.ReadAllText("/r/CHANGELOG.md");
        // Added comes before Fixed in the kind order
        var added = changelog.IndexOf("### Added", StringComparison.Ordinal);
        var fixedPos = changelog.IndexOf("### Fixed", StringComparison.Ordinal);
        Assert.True(added >= 0);
        Assert.True(fixedPos > added);
        Assert.Contains("- Added a feature.", changelog, StringComparison.Ordinal);
        Assert.Contains("- Fixed a bug.", changelog, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_is_idempotent()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["CHANGELOG.md"] = ChangelogSkeleton,
            ["docs/changes/BL-001.md"] = Fragment("BL-001", "Added", "2026-09-16",
                "- Added change fragments.\n"),
        });

        Run(fs);
        var afterFirst = fs.File.ReadAllText("/r/CHANGELOG.md");
        var okfAfterFirst = fs.File.ReadAllText("/r/docs/okf/log.md");

        Run(fs);
        var afterSecond = fs.File.ReadAllText("/r/CHANGELOG.md");
        var okfAfterSecond = fs.File.ReadAllText("/r/docs/okf/log.md");

        Assert.Equal(afterFirst, afterSecond);
        Assert.Equal(okfAfterFirst, okfAfterSecond);
    }

    [Fact]
    public void Pre_fragment_content_survives_in_changelog()
    {
        var existingChangelog = "# Changelog\n\n## [v25]\n\n- Historical entry.\n\n" + ChangelogSkeleton;
        var fs = Repo(new Dictionary<string, string>
        {
            ["CHANGELOG.md"] = existingChangelog,
            ["docs/changes/BL-001.md"] = Fragment("BL-001", "Added", "2026-09-16",
                "- Added change fragments.\n"),
        });

        var result = Run(fs);

        Assert.Equal(0, result.ExitCode);
        var changelog = fs.File.ReadAllText("/r/CHANGELOG.md");
        Assert.Contains("## [v25]", changelog, StringComparison.Ordinal);
        Assert.Contains("- Historical entry.", changelog, StringComparison.Ordinal);
        Assert.Contains("- Added change fragments.", changelog, StringComparison.Ordinal);
    }

    [Fact]
    public void Pre_fragment_content_survives_in_okf_log()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["docs/okf/log.md"] = "# OKF Log\n\n## 2026-08-01\n\n- Historical entry.\n\n",
            ["docs/changes/BL-001.md"] = Fragment("BL-001", "Added", "2026-09-16",
                okfLog: "New entry.\n"),
        });

        var result = Run(fs);

        Assert.Equal(0, result.ExitCode);
        var log = fs.File.ReadAllText("/r/docs/okf/log.md");
        Assert.Contains("- Historical entry.", log, StringComparison.Ordinal);
        Assert.Contains("New entry.", log, StringComparison.Ordinal);
    }

    [Fact]
    public void The_job_is_named_render() => Assert.Equal("render", new RenderJob().Name);

    [Fact]
    public void IsTaskBranch_detects_task_branches()
    {
        // Absent git = not a task branch
        var proc = new FakeProcessRunner();
        var options = new LegislatorOptions();
        var (isTask, reason) = RenderJob.IsTaskBranch(proc, options, "/r");
        Assert.False(isTask);
        Assert.Null(reason);
    }

    [Fact]
    public void ReadRenderedCases_returns_parsed_markers()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["CHANGELOG.md"] = "# Changelog\n\n## [Unreleased]\n\n### Added\n\n<!-- rendered: BL-001 -->\n- Entry.\n",
        });
        var layout = new RepoLayout(new LegislatorOptions(), "/r");
        var cases = RenderJob.ReadRenderedCases(fs, layout);

        Assert.Contains("BL-001", cases);
        Assert.Single(cases);
    }

    [Fact]
    public void Multiple_markers_from_different_views_are_collected()
    {
        var fs = Repo(new Dictionary<string, string>
        {
            ["CHANGELOG.md"] = "<!-- rendered: BL-001 -->\n",
            ["docs/okf/log.md"] = "<!-- rendered: BL-002 -->\n",
        });
        var layout = new RepoLayout(new LegislatorOptions(), "/r");
        var cases = RenderJob.ReadRenderedCases(fs, layout);

        Assert.Equal(2, cases.Count);
        Assert.Contains("BL-001", cases);
        Assert.Contains("BL-002", cases);
    }
}