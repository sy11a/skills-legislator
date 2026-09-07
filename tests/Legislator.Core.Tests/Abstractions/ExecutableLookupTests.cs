using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Abstractions;
using Legislator.Core.Options;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Core.Tests.Abstractions;

/// <summary>
/// `shutil.which`, as a seam. The format hook asks it before it runs a formatter, and it asks
/// through the injected environment and filesystem rather than through the process's own PATH,
/// so a test can build a machine where only one of the two toolchains exists (R-8204).
/// </summary>
public sealed class ExecutableLookupTests
{
    private static readonly LegislatorOptions Options = new();

    private static (MockFileSystem Fs, FakeEnvironment Env) Machine(string path, params string[] files)
    {
        var fs = new MockFileSystem();
        foreach (var file in files)
        {
            fs.AddFile(file, new MockFileData(""));
        }

        var env = new FakeEnvironment();
        env.Vars["PATH"] = path;
        return (fs, env);
    }

    [Fact]
    public void An_executable_on_path_is_found_at_its_full_path()
    {
        var (fs, env) = Machine("/usr/bin", "/usr/bin/dotnet");

        Assert.Equal("/usr/bin/dotnet", ExecutableLookup.Which(fs, env, Options, "dotnet"));
    }

    [Fact]
    public void The_first_directory_on_path_that_has_it_wins()
    {
        var (fs, env) = Machine("/first:/second", "/first/dotnet", "/second/dotnet");

        Assert.Equal("/first/dotnet", ExecutableLookup.Which(fs, env, Options, "dotnet"));
    }

    [Fact]
    public void An_executable_on_no_path_entry_is_not_found()
    {
        var (fs, env) = Machine("/usr/bin", "/usr/bin/npx");

        Assert.Null(ExecutableLookup.Which(fs, env, Options, "dotnet"));
    }

    [Fact]
    public void No_path_variable_at_all_finds_nothing()
    {
        var fs = new MockFileSystem();
        fs.AddFile("/usr/bin/dotnet", new MockFileData(""));

        Assert.Null(ExecutableLookup.Which(fs, new FakeEnvironment(), Options, "dotnet"));
    }

    /// <summary>An empty PATH entry is the current directory in a shell; here it is nothing, because a hook resolving a formatter out of the edited tree is a hazard, not a feature.</summary>
    [Fact]
    public void An_empty_path_entry_is_skipped()
    {
        var (fs, env) = Machine(":/usr/bin", "/usr/bin/dotnet");

        Assert.Equal("/usr/bin/dotnet", ExecutableLookup.Which(fs, env, Options, "dotnet"));
    }

    /// <summary>On a Windows-shaped machine the name carries no extension; the lookup tries the executable ones the options model declares (R-701's neighbourhood).</summary>
    [Fact]
    public void A_windows_shaped_machine_finds_the_name_under_an_executable_extension()
    {
        var (fs, env) = Machine("/tools", "/tools/dotnet.exe");

        Assert.Equal("/tools/dotnet.exe", ExecutableLookup.Which(fs, env, Options, "dotnet"));
    }
}
