using System.IO.Abstractions.TestingHelpers;
using Legislator.Core.Options;
using Legislator.Core.Tests.TestSupport;
using Xunit;

namespace Legislator.Core.Tests.Options;

public sealed class OptionsComposerTests
{
    private const string MachineFile = "/fake-home/.config/legislator/legislator.yaml";
    private const string InstanceFile = "/work/legislator.yaml";

    public static TheoryData<string> EveryKey => [.. LegislatorOptions.KeyMap.Keys];

    [Fact]
    public void No_layers_yields_defaults()
    {
        var options = OptionsComposer.Compose(Fs(), new FakeEnvironment(), null, null);

        Assert.Equal("cases", options.CasesDir.Value);
        Assert.Equal(OptionsLayer.Defaults, options.CasesDir.Source);
    }

    [Fact]
    public void Machine_file_overrides_default_and_stamps_source()
    {
        var fs = Fs((MachineFile, "cases_dir: work\n"));

        var options = OptionsComposer.Compose(fs, new FakeEnvironment(), MachineFile, null);

        Assert.Equal("work", options.CasesDir.Value);
        Assert.Equal(OptionsLayer.Machine, options.CasesDir.Source);
    }

    [Fact]
    public void Instance_beats_machine_and_env_beats_instance()
    {
        var env = new FakeEnvironment();
        env.Vars["LEGISLATOR_OKF_DEBT_DAYS"] = "7";
        var fs = Fs((MachineFile, "okf_debt_days: 60\ncases_dir: a\n"), (InstanceFile, "cases_dir: b\n"));

        var options = OptionsComposer.Compose(fs, env, MachineFile, InstanceFile);

        Assert.Equal("b", options.CasesDir.Value);
        Assert.Equal(OptionsLayer.Instance, options.CasesDir.Source);
        Assert.Equal(7, options.OkfDebtDays.Value);
        Assert.Equal(OptionsLayer.Environment, options.OkfDebtDays.Source);
    }

    [Fact]
    public void Absent_file_is_an_absent_layer_not_an_error()
    {
        var options = OptionsComposer.Compose(Fs(), new FakeEnvironment(), MachineFile, InstanceFile);

        Assert.Equal(OptionsLayer.Defaults, options.DocsDir.Source);
    }

    [Fact]
    public void List_option_from_yaml_sequence()
    {
        var fs = Fs((MachineFile, "build_dirs:\n  - out\n  - target\n"));

        var options = OptionsComposer.Compose(fs, new FakeEnvironment(), MachineFile, null);

        Assert.Equal(["out", "target"], options.BuildDirs.Value);
    }

    [Fact]
    public void List_option_from_env_is_comma_split()
    {
        var env = new FakeEnvironment();
        env.Vars["LEGISLATOR_BUILD_DIRS"] = "out,target";

        var options = OptionsComposer.Compose(Fs(), env, null, null);

        Assert.Equal(["out", "target"], options.BuildDirs.Value);
        Assert.Equal(OptionsLayer.Environment, options.BuildDirs.Source);
    }

    [Theory]
    [MemberData(nameof(EveryKey))]
    public void Every_key_is_settable_from_a_layer(string key)
    {
        // The composer's key → member application is hand-written (no reflection in Core);
        // this census proves no key falls through. Reflection is test-side only.
        var member = typeof(LegislatorOptions).GetProperty(LegislatorOptions.KeyMap[key])!;
        var value = member.PropertyType == typeof(OptionValue<int>) ? "7" : "x";
        var fs = Fs((InstanceFile, $"{key}: {value}\n"));

        var options = OptionsComposer.Compose(fs, new FakeEnvironment(), null, InstanceFile);

        var entry = Assert.Single(options.Enumerate(), e => e.Key == key);
        Assert.Equal(OptionsLayer.Instance, entry.Source);
    }

    [Fact]
    public void Unknown_key_fails_loud_naming_layer_and_key()
    {
        var fs = Fs((MachineFile, "case_dir: x\n"));

        var ex = Assert.Throws<OptionsException>(() => OptionsComposer.Compose(fs, new FakeEnvironment(), MachineFile, null));

        var error = Assert.Single(ex.Errors);
        Assert.Equal(OptionsLayer.Machine, error.Layer);
        Assert.Equal("case_dir", error.Key);
        Assert.Contains("unknown", error.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Bad_int_fails_loud()
    {
        var fs = Fs((MachineFile, "okf_debt_days: soon\n"));

        var ex = Assert.Throws<OptionsException>(() => OptionsComposer.Compose(fs, new FakeEnvironment(), MachineFile, null));

        Assert.Equal("okf_debt_days", Assert.Single(ex.Errors).Key);
    }

    [Fact]
    public void Nested_yaml_value_fails_loud_naming_the_key()
    {
        var fs = Fs((MachineFile, "build_dirs:\n  deep:\n    - out\n"));

        var ex = Assert.Throws<OptionsException>(() => OptionsComposer.Compose(fs, new FakeEnvironment(), MachineFile, null));

        var error = Assert.Single(ex.Errors);
        Assert.Equal(OptionsLayer.Machine, error.Layer);
        Assert.Equal("build_dirs", error.Key);
        Assert.Contains("nested", error.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Errors_from_every_layer_are_collected_first_layer_first()
    {
        var env = new FakeEnvironment();
        env.Vars["LEGISLATOR_GIT_TIMEOUT_SECONDS"] = "0";
        var fs = Fs((MachineFile, "case_dir: x\n"), (InstanceFile, "docs_dir: ''\n"));

        var ex = Assert.Throws<OptionsException>(() => OptionsComposer.Compose(fs, env, MachineFile, InstanceFile));

        Assert.Equal(
            [OptionsLayer.Machine, OptionsLayer.Instance, OptionsLayer.Environment],
            ex.Errors.Select(e => e.Layer).ToList());
        Assert.Equal(["case_dir", "docs_dir", "git_timeout_seconds"], ex.Errors.Select(e => e.Key).ToList());
    }

    private static MockFileSystem Fs(params (string Path, string Text)[] files)
    {
        var fs = new MockFileSystem();
        foreach (var (path, text) in files)
        {
            fs.AddFile(path, new MockFileData(text));
        }

        return fs;
    }
}
