using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Options;
using Legislator.Hooks.Hooks;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Hooks.Tests.Hooks;

/// <summary>
/// The write-guard on owned law, branch by documented branch (C-11). The Python's docstring is
/// the contract: a legislated repository is one with a manifest up the tree, and inside it the
/// rules directory, the delivered engine and the root wiring file are blocked while the
/// manifest itself deliberately is not. Everything else - including every case where the hook
/// cannot tell - allows, because a hook that stops the user's work is worse than a hand-edit.
/// </summary>
public sealed class GuardOwnedFilesHookTests
{
    private static readonly LegislatorOptions Options = new();

    /// <summary>A legislated repository at <c>/r</c>: a manifest, one delivered rule, the wiring file and the engine.</summary>
    private static MockFileSystem Legislated()
    {
        var fs = new MockFileSystem();
        fs.AddFile("/r/docs/ai/manifest.json", new MockFileData("{}"));
        fs.AddFile("/r/docs/ai/rules/core/okf.md", new MockFileData("## X\n"));
        fs.AddFile("/r/docs/ai/engine.py", new MockFileData("# engine\n"));
        fs.AddFile("/r/opencode.json", new MockFileData("{}"));
        return fs;
    }

    private static HookResult Judge(MockFileSystem fs, string raw) =>
        new GuardOwnedFilesHook().Run(
            new HookContext(HookPayload.Parse(raw), fs, new FakeEnvironment(), new FakeProcessRunner(), Options));

    /// <summary>The Claude Code Edit payload the ruler pipes, with the path the case is about.</summary>
    private static string Edit(string filePath, string key = "file_path") =>
        $$"""
        {"hook_event_name": "PreToolUse", "tool_name": "Edit",
         "tool_input": {"{{key}}": "{{filePath}}", "old_string": "a", "new_string": "b"},
         "cwd": "/r"}
        """;

    [Fact]
    public void An_owned_rule_file_in_a_legislated_repo_is_blocked()
    {
        var result = Judge(Legislated(), Edit("/r/docs/ai/rules/core/okf.md"));

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("machine-managed law", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_delivered_engine_is_blocked()
    {
        Assert.Equal(2, Judge(Legislated(), Edit("/r/docs/ai/engine.py")).ExitCode);
    }

    [Fact]
    public void The_root_wiring_file_is_blocked()
    {
        Assert.Equal(2, Judge(Legislated(), Edit("/r/opencode.json")).ExitCode);
    }

    /// <summary>Deliberately unguarded: the apply job regenerates the manifest on every run, and that rewrite already heals a hand-edit (the hook's own docstring).</summary>
    [Fact]
    public void The_manifest_itself_is_not_guarded()
    {
        Assert.Equal(0, Judge(Legislated(), Edit("/r/docs/ai/manifest.json")).ExitCode);
    }

    [Fact]
    public void An_ordinary_file_in_a_legislated_repo_is_allowed()
    {
        Assert.Equal(0, Judge(Legislated(), Edit("/r/src/a.cs")).ExitCode);
    }

    /// <summary>A root config the legislator does not own - the guard names its files, it does not guard the root.</summary>
    [Fact]
    public void An_unowned_root_config_is_allowed()
    {
        var fs = Legislated();
        fs.AddFile("/r/package.json", new MockFileData("{}"));

        Assert.Equal(0, Judge(fs, Edit("/r/package.json")).ExitCode);
    }

    [Fact]
    public void An_unowned_file_beside_the_owned_ones_is_allowed()
    {
        var fs = Legislated();
        fs.AddFile("/r/docs/ai/notes.md", new MockFileData("# notes\n"));

        Assert.Equal(0, Judge(fs, Edit("/r/docs/ai/notes.md")).ExitCode);
    }

    /// <summary>The same rules-shaped path with no manifest anywhere upward: not a legislated repository, so nothing here is owned.</summary>
    [Fact]
    public void A_rules_shaped_path_with_no_manifest_is_allowed()
    {
        var fs = new MockFileSystem();
        fs.AddFile("/plain/docs/ai/rules/core/okf.md", new MockFileData("## X\n"));

        Assert.Equal(0, Judge(fs, Edit("/plain/docs/ai/rules/core/okf.md")).ExitCode);
    }

    /// <summary>The manifest is looked for UP the tree, not only beside the file - a rule nested three directories deep is owned by the repository above it.</summary>
    [Fact]
    public void The_manifest_is_found_far_above_the_edited_file()
    {
        var fs = Legislated();
        fs.AddFile("/r/docs/ai/rules/stacks/dotnet/deep/x.md", new MockFileData("## X\n"));

        Assert.Equal(2, Judge(fs, Edit("/r/docs/ai/rules/stacks/dotnet/deep/x.md")).ExitCode);
    }

    /// <summary>A notebook edit names its path under another key; the guard reads both or it guards nothing when the tool changes.</summary>
    [Fact]
    public void A_notebook_path_is_read_as_the_edited_file()
    {
        var fs = Legislated();
        fs.AddFile("/r/docs/ai/rules/core/nb.md", new MockFileData("## X\n"));

        Assert.Equal(2, Judge(fs, Edit("/r/docs/ai/rules/core/nb.md", key: "notebook_path")).ExitCode);
    }

    /// <summary>A relative path is resolved against the payload's cwd, not against the process's - the hook runs wherever Claude Code started it.</summary>
    [Fact]
    public void A_relative_path_is_resolved_against_the_payloads_cwd()
    {
        var raw =
            """
            {"tool_name": "Edit", "tool_input": {"file_path": "docs/ai/rules/core/okf.md"}, "cwd": "/r"}
            """;

        Assert.Equal(2, Judge(Legislated(), raw).ExitCode);
    }

    [Fact]
    public void Malformed_stdin_allows()
    {
        Assert.Equal(0, Judge(Legislated(), "not json").ExitCode);
    }

    [Fact]
    public void Empty_stdin_allows()
    {
        Assert.Equal(0, Judge(Legislated(), "").ExitCode);
    }

    /// <summary>Valid JSON that is not an object at all - the payload shape is a decision the hook makes, never an exception it takes.</summary>
    [Fact]
    public void A_json_array_on_stdin_allows()
    {
        Assert.Equal(0, Judge(Legislated(), "[1, 2, 3]").ExitCode);
    }

    [Fact]
    public void A_payload_without_tool_input_allows()
    {
        Assert.Equal(0, Judge(Legislated(), """{"tool_name": "Edit"}""").ExitCode);
    }

    /// <summary>`tool_input` present but of the wrong shape: the Python asks whether it is a dict and allows when it is not.</summary>
    [Fact]
    public void A_tool_input_that_is_not_an_object_allows()
    {
        Assert.Equal(0, Judge(Legislated(), """{"tool_name": "Edit", "tool_input": "nonsense"}""").ExitCode);
    }

    [Fact]
    public void A_tool_input_without_a_path_allows()
    {
        Assert.Equal(0, Judge(Legislated(), """{"tool_name": "Edit", "tool_input": {"old_string": "a"}}""").ExitCode);
    }

    /// <summary>A path of the wrong type is not a path - the Python demands a string before it touches the filesystem.</summary>
    [Fact]
    public void A_path_that_is_not_a_string_allows()
    {
        Assert.Equal(0, Judge(Legislated(), """{"tool_name": "Edit", "tool_input": {"file_path": 42}}""").ExitCode);
    }
}
