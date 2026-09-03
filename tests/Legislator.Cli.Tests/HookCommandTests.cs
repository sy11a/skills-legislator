using System.IO.Abstractions.TestingHelpers;
using Legislator.Engine;
using Legislator.Hooks;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Cli.Tests;

/// <summary>
/// The host's side of the hook contract (C-11): `legislator hook &lt;name&gt;` reads one JSON
/// object on stdin and answers 0 or 2 - never another code, never an exception escaping. The
/// catch-all lives here rather than in each hook because it is the CONTRACT that must hold,
/// including for a hook whose own bug the contract cannot foresee.
/// </summary>
public sealed class HookCommandTests : IDisposable
{
    private readonly MockFileSystem fs = new();
    private readonly FakeEnvironment env = new();
    private readonly FakeProcessRunner proc = new();
    private readonly StringWriter stdout = new();
    private readonly StringWriter stderr = new();

    public void Dispose()
    {
        stdout.Dispose();
        stderr.Dispose();
    }

    /// <summary>A hook of the test's own making, so the contract is pinned without a real hook's behaviour in the way.</summary>
    private sealed class Scripted(string name, Func<HookContext, HookResult> run) : IHook
    {
        public string Name { get; } = name;

        public HookResult Run(HookContext ctx) => run(ctx);
    }

    private int Run(string stdin, params string[] args) => Program.Run(
        args, JobRegistry.Jobs, Hooks(), fs, TimeProvider.System, env, proc,
        new StringReader(stdin), stdout, stderr);

    private static Dictionary<string, Func<IHook>> Hooks() => new(StringComparer.Ordinal)
    {
        ["allowing"] = () => new Scripted("allowing", _ => HookResult.Allow),
        ["blocking"] = () => new Scripted("blocking", _ => HookResult.Block("the reason")),
        ["throwing"] = () => new Scripted("throwing", _ => throw new InvalidOperationException("a bug in the hook")),
    };

    [Fact]
    public void An_allowing_hook_exits_0_and_says_nothing()
    {
        Assert.Equal(0, Run("{}", "hook", "allowing"));
        Assert.Equal("", stdout.ToString());
        Assert.Equal("", stderr.ToString());
    }

    /// <summary>The block message goes to stderr, where Claude Code feeds it back to the model - stdout stays empty so the hook never speaks to the user's terminal.</summary>
    [Fact]
    public void A_blocking_hook_exits_2_with_its_message_on_stderr()
    {
        Assert.Equal(2, Run("{}", "hook", "blocking"));
        Assert.Equal("the reason", stderr.ToString());
        Assert.Equal("", stdout.ToString());
    }

    /// <summary>The contract's hard edge: a hook that throws must not stop the user's work, so its bug becomes an allow rather than the host's exit 3.</summary>
    [Fact]
    public void A_hook_that_throws_exits_0()
    {
        Assert.Equal(0, Run("{}", "hook", "throwing"));
        Assert.Equal("", stderr.ToString());
    }

    /// <summary>A misspelled name in hooks.json is a configuration fault, and a silent one would leave the guard disabled with nobody the wiser.</summary>
    [Fact]
    public void An_unknown_hook_name_is_usage_exit_2_naming_it()
    {
        Assert.Equal(2, Run("{}", "hook", "nosuchhook"));
        Assert.Contains("nosuchhook", stderr.ToString(), StringComparison.Ordinal);
        Assert.Contains("usage: legislator", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Hook_without_a_name_is_usage_exit_2()
    {
        Assert.Equal(2, Run("{}", "hook"));
        Assert.Contains("usage: legislator", stderr.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Stdin that is not JSON reaches the hook as an unusable payload; the decision stays the hook's, and every shipped one allows.</summary>
    [Fact]
    public void Malformed_stdin_reaches_the_hook_as_an_unusable_payload()
    {
        var seen = false;
        var hooks = new Dictionary<string, Func<IHook>>(StringComparer.Ordinal)
        {
            ["probe"] = () => new Scripted("probe", ctx =>
            {
                seen = ctx.Payload.Usable;
                return HookResult.Allow;
            }),
        };

        Assert.Equal(
            0,
            Program.Run(
                ["hook", "probe"], JobRegistry.Jobs, hooks, fs, TimeProvider.System, env, proc,
                new StringReader("not json"), stdout, stderr));
        Assert.False(seen);
    }

    /// <summary>The usage text names `hook` beside the jobs, and lists the hooks the host was handed - a reader of `legislator` with no arguments learns both.</summary>
    [Fact]
    public void The_usage_text_names_the_hook_command_and_the_hooks()
    {
        Program.Run(
            ["hook"], JobRegistry.Jobs, HookRegistry.Hooks, fs, TimeProvider.System, env, proc,
            TextReader.Null, stdout, stderr);

        Assert.Contains("legislator hook", stderr.ToString(), StringComparison.Ordinal);
        Assert.Contains("guard_owned_files", stderr.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Every shipped hook satisfies the contract on a payload none of them expects - the sweep no single hook's own tests can make.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Every_shipped_hook_answers_0_or_2_on_any_stdin(string stdin)
    {
        foreach (var name in HookRegistry.Names)
        {
            var exit = Program.Run(
                ["hook", name], JobRegistry.Jobs, HookRegistry.Hooks, new MockFileSystem(),
                TimeProvider.System, new FakeEnvironment(), new FakeProcessRunner(),
                new StringReader(stdin), new StringWriter(), new StringWriter());

            Assert.True(exit is 0 or 2, $"{name} exited {exit} on stdin \"{stdin}\"");
        }
    }
}
