using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Legislator.Parity.Tests.Hooks;

/// <summary>The named twins of every `check_hooks.py` assertion the owned-file guard answers (R-8206).</summary>
public sealed class GuardOwnedFilesTwins
{
    private const string Hook = "guard_owned_files";

    private static (int Exit, string Out, string Err) Guard(MockFileSystem fs, string filePath) =>
        RunHooks.Run(Hook, RunHooks.EditPayload(filePath), fs);

    [Fact]
    [Parity("hooks", "owned rule file in legislated repo blocked (exit 2)")]
    [Parity("hooks", "block message mentions machine-managed law")]
    public void Owned_rule_file_in_legislated_repo_blocked()
    {
        var (exit, _, err) = Guard(RunHooks.LegislatedRepo(), $"{RunHooks.Root}/docs/ai/rules/core/x.md");

        Assert.Equal(2, exit);
        Assert.Contains("machine-managed law", err, StringComparison.Ordinal);
    }

    [Fact]
    [Parity("hooks", "non-rules file in legislated repo allowed (exit 0)")]
    public void Non_rules_file_in_legislated_repo_allowed()
    {
        var (exit, _, err) = Guard(RunHooks.LegislatedRepo(), $"{RunHooks.Root}/src/a.cs");

        Assert.Equal(0, exit);
        Assert.Equal("", err);
    }

    [Fact]
    [Parity("hooks", "owned root opencode.json blocked (exit 2)")]
    public void Owned_root_opencode_json_blocked()
    {
        var fs = RunHooks.LegislatedRepo();
        fs.AddFile($"{RunHooks.Root}/opencode.json", new MockFileData("{}"));

        Assert.Equal(2, Guard(fs, $"{RunHooks.Root}/opencode.json").Exit);
    }

    [Fact]
    [Parity("hooks", "non-owned root package.json allowed (exit 0)")]
    public void Non_owned_root_package_json_allowed()
    {
        var fs = RunHooks.LegislatedRepo();
        fs.AddFile($"{RunHooks.Root}/package.json", new MockFileData("{}"));

        Assert.Equal(0, Guard(fs, $"{RunHooks.Root}/package.json").Exit);
    }

    [Fact]
    [Parity("hooks", "retired docs/ai/engine.py is an ordinary file (exit 0)")]
    public void Retired_engine_py_is_an_ordinary_file()
    {
        var fs = RunHooks.LegislatedRepo();
        fs.AddFile($"{RunHooks.Root}/docs/ai/engine.py", new MockFileData("# engine\n"));

        var (exit, _, err) = Guard(fs, $"{RunHooks.Root}/docs/ai/engine.py");

        Assert.Equal(0, exit);
        Assert.Equal(string.Empty, err);
    }

    [Fact]
    [Parity("hooks", "unowned docs/ai/notes.md allowed (exit 0)")]
    public void Unowned_docs_ai_notes_md_allowed()
    {
        var fs = RunHooks.LegislatedRepo();
        fs.AddFile($"{RunHooks.Root}/docs/ai/notes.md", new MockFileData("# notes\n"));

        Assert.Equal(0, Guard(fs, $"{RunHooks.Root}/docs/ai/notes.md").Exit);
    }

    [Fact]
    [Parity("hooks", "rules-shaped path with no manifest allowed (exit 0)")]
    public void Rules_shaped_path_with_no_manifest_allowed()
    {
        var fs = new MockFileSystem();
        fs.AddFile("/plain-repo/docs/ai/rules/core/x.md", new MockFileData("## X\n"));

        Assert.Equal(0, Guard(fs, "/plain-repo/docs/ai/rules/core/x.md").Exit);
    }

    [Fact]
    [Parity("hooks", "guard_owned_files: malformed stdin allowed (exit 0)")]
    public void Malformed_stdin_allowed()
    {
        Assert.Equal(0, RunHooks.Run(Hook, "not json", RunHooks.LegislatedRepo()).Exit);
    }

    [Fact]
    [Parity("hooks", "empty stdin allowed (exit 0)")]
    public void Empty_stdin_allowed()
    {
        Assert.Equal(0, RunHooks.Run(Hook, "", RunHooks.LegislatedRepo()).Exit);
    }
}
