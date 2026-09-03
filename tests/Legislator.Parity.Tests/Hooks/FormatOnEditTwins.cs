using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Legislator.Parity.Tests.Hooks;

/// <summary>The named twins of every `check_hooks.py` assertion the format hook answers (R-8206).</summary>
public sealed class FormatOnEditTwins
{
    private const string Hook = "format_on_edit";

    /// <summary>The ruler's temporary directory: a lone file with no project and no config anywhere above it.</summary>
    private static MockFileSystem Loose(string name, string text)
    {
        var fs = new MockFileSystem();
        fs.AddFile($"/tmp/{name}", new MockFileData(text));
        return fs;
    }

    [Fact]
    [Parity("hooks", ".cs file with no dotnet project: exit 0")]
    [Parity("hooks", ".cs file with no dotnet project: no output")]
    public void Cs_file_with_no_dotnet_project_exits_0_silently()
    {
        var fs = Loose("foo.cs", "class Foo {}\n");

        var (exit, output, err) = RunHooks.Run(Hook, RunHooks.EditPayload("/tmp/foo.cs"), fs);

        Assert.Equal(0, exit);
        Assert.Equal("", output);
        Assert.Equal("", err);
    }

    [Fact]
    [Parity("hooks", "non-code file: exit 0")]
    public void Non_code_file_exits_0()
    {
        var fs = Loose("notes.txt", "hello\n");

        Assert.Equal(0, RunHooks.Run(Hook, RunHooks.EditPayload("/tmp/notes.txt"), fs).Exit);
    }

    [Fact]
    [Parity("hooks", "format_on_edit: malformed stdin allowed (exit 0)")]
    public void Malformed_stdin_allowed()
    {
        Assert.Equal(0, RunHooks.Run(Hook, "not json", new MockFileSystem()).Exit);
    }
}
