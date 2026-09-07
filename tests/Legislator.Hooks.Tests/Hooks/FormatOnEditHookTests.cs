using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.Hooks.Hooks;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Hooks.Tests.Hooks;

/// <summary>
/// Best-effort formatting of a just-edited file (C-11). This hook has exactly one exit code -
/// 0 - and its whole contract is what it RUNS, not what it returns: a missing toolchain, a
/// missing project, a formatter that fails or hangs all end the same way and silently. The
/// tests therefore assert on the recorded process calls, because the exit code alone cannot
/// tell a formatter that ran from one that was never reached.
/// </summary>
public sealed class FormatOnEditHookTests
{
    private static readonly LegislatorOptions Options = new();

    /// <summary>An environment whose PATH holds every executable the test declares, at <c>/bin</c>.</summary>
    private static (MockFileSystem Fs, FakeEnvironment Env) Machine(params string[] executables)
    {
        var fs = new MockFileSystem();
        var env = new FakeEnvironment();
        env.Vars["PATH"] = "/bin";
        foreach (var name in executables)
        {
            fs.AddFile($"/bin/{name}", new MockFileData(""));
        }

        return (fs, env);
    }

    private static FakeProcessRunner Format(MockFileSystem fs, FakeEnvironment env, string filePath)
    {
        var proc = new FakeProcessRunner();
        var raw = $$"""{"tool_name": "Edit", "tool_input": {"file_path": "{{filePath}}"}, "cwd": "/r"}""";
        var result = new FormatOnEditHook().Run(new HookContext(HookPayload.Parse(raw), fs, env, proc, Options));

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Message);
        return proc;
    }

    [Fact]
    public void A_cs_file_with_a_project_above_it_is_formatted_by_dotnet()
    {
        var (fs, env) = Machine("dotnet");
        fs.AddFile("/r/App/App.csproj", new MockFileData("<Project />"));
        fs.AddFile("/r/App/Widget.cs", new MockFileData("class Widget { }\n"));

        var call = Assert.Single(Format(fs, env, "/r/App/Widget.cs").Calls);

        Assert.Equal("/bin/dotnet", call.FileName);
        Assert.Equal(["format", "/r/App/App.csproj", "--include", "/r/App/Widget.cs"], call.Args);
    }

    /// <summary>The project is looked for UP the tree, the way the Python walks its parents.</summary>
    [Fact]
    public void The_project_is_found_above_the_edited_file()
    {
        var (fs, env) = Machine("dotnet");
        fs.AddFile("/r/App.sln", new MockFileData(""));
        fs.AddFile("/r/deep/nested/Widget.cs", new MockFileData("class Widget { }\n"));

        var call = Assert.Single(Format(fs, env, "/r/deep/nested/Widget.cs").Calls);

        Assert.Equal(["format", "/r/App.sln", "--include", "/r/deep/nested/Widget.cs"], call.Args);
    }

    [Fact]
    public void A_cs_file_with_no_project_anywhere_above_it_runs_nothing()
    {
        var (fs, env) = Machine("dotnet");
        fs.AddFile("/r/Widget.cs", new MockFileData("class Widget { }\n"));

        Assert.Empty(Format(fs, env, "/r/Widget.cs").Calls);
    }

    [Fact]
    public void A_cs_file_runs_nothing_when_dotnet_is_not_on_path()
    {
        var (fs, env) = Machine();
        fs.AddFile("/r/App/App.csproj", new MockFileData("<Project />"));
        fs.AddFile("/r/App/Widget.cs", new MockFileData("class Widget { }\n"));

        Assert.Empty(Format(fs, env, "/r/App/Widget.cs").Calls);
    }

    [Fact]
    public void A_prettier_file_with_a_config_above_it_is_formatted_by_npx()
    {
        var (fs, env) = Machine("npx");
        fs.AddFile("/r/.prettierrc", new MockFileData("{}"));
        fs.AddFile("/r/web/app.ts", new MockFileData("const a = 1\n"));

        var call = Assert.Single(Format(fs, env, "/r/web/app.ts").Calls);

        Assert.Equal("/bin/npx", call.FileName);
        Assert.Equal(["prettier", "--write", "/r/web/app.ts"], call.Args);
    }

    /// <summary>A package.json carrying a `prettier` key is a prettier config; one without it is not.</summary>
    [Fact]
    public void A_package_json_is_a_config_only_when_it_carries_the_prettier_key()
    {
        var (withKey, env) = Machine("npx");
        withKey.AddFile("/r/package.json", new MockFileData("""{"prettier": {"semi": false}}"""));
        withKey.AddFile("/r/app.js", new MockFileData("const a = 1\n"));
        Assert.Single(Format(withKey, env, "/r/app.js").Calls);

        var (without, env2) = Machine("npx");
        without.AddFile("/r/package.json", new MockFileData("""{"name": "x"}"""));
        without.AddFile("/r/app.js", new MockFileData("const a = 1\n"));
        Assert.Empty(Format(without, env2, "/r/app.js").Calls);
    }

    /// <summary>A package.json that is not JSON at all is not a config either - and is not an exception.</summary>
    [Fact]
    public void An_unparseable_package_json_is_not_a_config()
    {
        var (fs, env) = Machine("npx");
        fs.AddFile("/r/package.json", new MockFileData("not json"));
        fs.AddFile("/r/app.js", new MockFileData("const a = 1\n"));

        Assert.Empty(Format(fs, env, "/r/app.js").Calls);
    }

    [Fact]
    public void A_prettier_file_runs_nothing_when_npx_is_not_on_path()
    {
        var (fs, env) = Machine();
        fs.AddFile("/r/.prettierrc", new MockFileData("{}"));
        fs.AddFile("/r/app.ts", new MockFileData("const a = 1\n"));

        Assert.Empty(Format(fs, env, "/r/app.ts").Calls);
    }

    [Fact]
    public void A_file_of_no_known_kind_runs_nothing()
    {
        var (fs, env) = Machine("dotnet", "npx");
        fs.AddFile("/r/notes.txt", new MockFileData("hello\n"));

        Assert.Empty(Format(fs, env, "/r/notes.txt").Calls);
    }

    /// <summary>A formatter that fails is still best-effort polish: the hook swallows the exit code and says nothing.</summary>
    [Fact]
    public void A_formatter_that_fails_is_swallowed()
    {
        var (fs, env) = Machine("dotnet");
        fs.AddFile("/r/App.csproj", new MockFileData("<Project />"));
        fs.AddFile("/r/Widget.cs", new MockFileData("class Widget { }\n"));
        var proc = new FakeProcessRunner { OnRun = (_, _, _) => new ProcessResult(1, "", "boom") };
        var raw = """{"tool_name": "Edit", "tool_input": {"file_path": "/r/Widget.cs"}, "cwd": "/r"}""";

        var result = new FormatOnEditHook().Run(new HookContext(HookPayload.Parse(raw), fs, env, proc, Options));

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", result.Message);
    }

    /// <summary>A formatter that cannot be started at all is the same silence - the `which` check races the filesystem and may win.</summary>
    [Fact]
    public void A_formatter_that_cannot_be_started_is_swallowed()
    {
        var (fs, env) = Machine("dotnet");
        fs.AddFile("/r/App.csproj", new MockFileData("<Project />"));
        fs.AddFile("/r/Widget.cs", new MockFileData("class Widget { }\n"));
        var proc = new FakeProcessRunner { OnRun = (_, _, _) => throw new ProcessStartException("dotnet", new IOException("gone")) };
        var raw = """{"tool_name": "Edit", "tool_input": {"file_path": "/r/Widget.cs"}, "cwd": "/r"}""";

        var result = new FormatOnEditHook().Run(new HookContext(HookPayload.Parse(raw), fs, env, proc, Options));

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void Malformed_stdin_runs_nothing_and_allows()
    {
        var (fs, env) = Machine("dotnet", "npx");
        var proc = new FakeProcessRunner();

        var result = new FormatOnEditHook().Run(new HookContext(HookPayload.Parse("not json"), fs, env, proc, Options));

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(proc.Calls);
    }
}
