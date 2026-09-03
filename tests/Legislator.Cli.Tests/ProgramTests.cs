using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Legislator.Core.Options;
using Legislator.TestSupport;
using Legislator.Engine;
using Legislator.Hooks;
using Xunit;

namespace Legislator.Cli.Tests;

/// <summary>Drives <see cref="Program.Run"/> in-process with fakes: the exit-code contract, dispatch, <c>config show</c> and <c>version</c> (C-05).</summary>
public sealed class ProgramTests : IDisposable
{
    private const string MachineFile = "/fake-home/.config/legislator/legislator.yaml";
    private const string InstanceFile = "/work/legislator.yaml";

    private readonly MockFileSystem fs = new();
    private readonly FakeEnvironment env = new();
    private readonly FakeProcessRunner proc = new();
    private readonly Dictionary<string, Func<IJob>> jobs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Func<IHook>> hooks = new(StringComparer.Ordinal);
    private readonly StringWriter stdout = new();
    private readonly StringWriter stderr = new();

    [Fact]
    public void No_args_is_usage_exit_2()
    {
        Assert.Equal(2, Run());
        Assert.StartsWith("usage: legislator", stderr.ToString(), StringComparison.Ordinal);
        Assert.Equal("", stdout.ToString());
    }

    [Fact]
    public void Unknown_job_is_usage_exit_2_naming_it()
    {
        Assert.Equal(2, Run("dance"));
        Assert.Contains("dance", stderr.ToString(), StringComparison.Ordinal);
        Assert.Contains("usage: legislator", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Config_without_show_is_usage_exit_2()
    {
        Assert.Equal(2, Run("config"));
        Assert.Contains("usage: legislator", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Version_prints_the_pinned_version()
    {
        Assert.Equal(0, Run("version"));
        Assert.Matches(@"^\d+\.\d+\.\d+\n$", stdout.ToString());
        Assert.Equal("", stderr.ToString());
    }

    [Fact]
    public void Config_show_prints_every_option_with_source()
    {
        Assert.Equal(0, Run("config", "show"));

        var lines = stdout.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("cases_dir = cases  [defaults]", lines);
        Assert.Equal(new LegislatorOptions().Enumerate().Count(), lines.Length);
    }

    [Fact]
    public void Config_show_reads_the_instance_file_under_root()
    {
        fs.AddFile("/r/legislator.yaml", new MockFileData("cases_dir: work\n"));

        Assert.Equal(0, Run("config", "show", "--root", "/r"));
        Assert.Contains("cases_dir = work  [instance]", stdout.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Config_show_reads_the_machine_file_under_home()
    {
        fs.AddFile(MachineFile, new MockFileData("okf_debt_days: 7\n"));

        Assert.Equal(0, Run("config", "show"));
        Assert.Contains("okf_debt_days = 7  [machine]", stdout.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Config_show_json_is_one_object_of_every_option()
    {
        Assert.Equal(0, Run("config", "show", "--json"));

        using var doc = JsonDocument.Parse(stdout.ToString());
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        Assert.Equal(new LegislatorOptions().Enumerate().Count(), doc.RootElement.EnumerateObject().Count());
        Assert.Equal("cases", doc.RootElement.GetProperty("cases_dir").GetProperty("value").GetString());
        Assert.Equal("defaults", doc.RootElement.GetProperty("cases_dir").GetProperty("source").GetString());
    }

    [Fact]
    public void Config_error_exits_2_naming_layer_key_reason()
    {
        fs.AddFile(MachineFile, new MockFileData("nope: 1\n"));

        Assert.Equal(2, Run("config", "show"));
        Assert.Contains("machine: nope: unknown key", stderr.ToString(), StringComparison.Ordinal);
        Assert.Equal("", stdout.ToString());
    }

    [Fact]
    public void Stray_environment_variable_is_a_config_error()
    {
        env.Vars["LEGISLATOR_NOPE"] = "1";

        Assert.Equal(2, Run("config", "show"));
        Assert.Contains("environment: nope: unknown key", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Config_fault_stops_before_the_job_runs()
    {
        var runs = 0;
        jobs["probe"] = () => new StubJob(_ => { runs++; return new JobResult(0, "", ""); });
        fs.AddFile(InstanceFile, new MockFileData("okf_debt_days: zero\n"));

        Assert.Equal(2, Run("probe"));
        Assert.Equal(0, runs);
        Assert.Contains("instance: okf_debt_days: must be an integer of 1 or more", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Job_result_passes_through_exit_code_and_streams()
    {
        jobs["probe"] = () => new StubJob(_ => new JobResult(1, "finding\n", "note\n"));

        Assert.Equal(1, Run("probe"));
        Assert.Equal("finding\n", stdout.ToString());
        Assert.Equal("note\n", stderr.ToString());
    }

    [Fact]
    public void Job_receives_root_remaining_args_options_and_services()
    {
        JobContext? seen = null;
        jobs["probe"] = () => new StubJob(ctx => { seen = ctx; return new JobResult(0, "", ""); });

        Assert.Equal(0, Run("probe", "--root", "/r", "x", "--flag"));

        Assert.NotNull(seen);
        Assert.Equal("/r", seen.Root);
        Assert.Equal(["x", "--flag"], seen.Args);
        Assert.Equal("cases", seen.Options.CasesDir.Value);
        Assert.Same(fs, seen.Fs);
        Assert.Same(env, seen.Env);
        Assert.Same(proc, seen.Proc);
        Assert.Same(TimeProvider.System, seen.Clock);
    }

    [Fact]
    public void Root_defaults_to_the_current_directory()
    {
        JobContext? seen = null;
        jobs["probe"] = () => new StubJob(ctx => { seen = ctx; return new JobResult(0, "", ""); });

        Assert.Equal(0, Run("probe"));
        Assert.Equal(env.CurrentDirectory, seen!.Root);
    }

    [Fact]
    public void Root_without_a_value_is_usage_exit_2()
    {
        jobs["probe"] = () => new StubJob(_ => new JobResult(0, "", ""));

        Assert.Equal(2, Run("probe", "--root"));
        Assert.Contains("usage: legislator", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Job_exception_is_exit_3_with_reason()
    {
        jobs["boom"] = () => new StubJob(_ => throw new InvalidOperationException("kaput"));

        Assert.Equal(3, Run("boom"));
        Assert.StartsWith("engine failed: InvalidOperationException: kaput", stderr.ToString(), StringComparison.Ordinal);
    }

    public void Dispose()
    {
        stdout.Dispose();
        stderr.Dispose();
    }

    private int Run(params string[] args) => Program.Run(
        args, jobs, hooks, fs, TimeProvider.System, env, proc, TextReader.Null, stdout, stderr);

    private sealed class StubJob(Func<JobContext, JobResult> run) : IJob
    {
        public string Name => "probe";

        public string Usage => "probe [args]";

        public JobResult Run(JobContext ctx) => run(ctx);
    }
}
