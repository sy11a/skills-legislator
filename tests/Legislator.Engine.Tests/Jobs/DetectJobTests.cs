using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Legislator.Core.Options;
using Legislator.Engine;
using Legislator.Engine.Jobs;
using Legislator.TestSupport;
using Xunit;

namespace Legislator.Engine.Tests.Jobs;

/// <summary>
/// The detect job branch by branch - Step 1's decision tree read off a tree that is only a
/// tree. The ruler reaches five of these; the rest are the branches a real repository makes
/// awkward to build and a fake makes cheap: an entry alias that is a symlink, a manifest that
/// does not parse, a package.json that does not parse, the modern stacks key beside the legacy
/// profiles one.
/// </summary>
public sealed class DetectJobTests
{
    private const string SkillPath = "/skill";

    private static MockFileSystem Repo(Dictionary<string, string> files, string version = "25")
    {
        var fs = new MockFileSystem();
        fs.AddDirectory("/r");
        foreach (var (rel, text) in files)
        {
            fs.AddFile($"/r/{rel}", new MockFileData(text));
        }

        fs.AddFile($"{SkillPath}/VERSION", new MockFileData($"{version}\n"));
        return fs;
    }

    private static JobResult Run(IFileSystem fs, params string[] args) =>
        new DetectJob().Run(new JobContext(
            fs, TimeProvider.System, new FakeEnvironment(), new FakeProcessRunner(),
            new LegislatorOptions(), "/r", args));

    private static JobResult Detect(Dictionary<string, string> files) =>
        Run(Repo(files), "--skill", SkillPath);

    private static JsonElement Json(JobResult result) => JsonDocument.Parse(result.Stdout).RootElement;

    private static string? Text(JsonElement json, string name) =>
        json.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    [Fact]
    public void Given_no_skill_flag_When_detect_runs_Then_it_names_the_flag_and_exits_2()
    {
        var result = Run(Repo([]));

        Assert.Equal(2, result.ExitCode);
        Assert.Equal("detect requires --skill <skill-path> (the legislator package root)\n", result.Stderr);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    public void Given_a_skill_path_that_is_not_a_directory_When_detect_runs_Then_it_exits_2()
    {
        var fs = Repo([]);
        fs.AddFile("/not-a-dir", new MockFileData("x"));

        var result = Run(fs, "--skill", "/not-a-dir");

        Assert.Equal(2, result.ExitCode);
        Assert.Equal("detect requires --skill <skill-path> (the legislator package root)\n", result.Stderr);
    }

    [Fact]
    public void Given_both_entry_documents_When_detect_runs_Then_the_canonical_one_wins()
    {
        var json = Json(Detect(new() { ["AGENTS.md"] = "# P\n", ["CLAUDE.md"] = "# P\n" }));

        Assert.Equal("AGENTS.md", Text(json, "entry"));
    }

    [Fact]
    public void Given_the_alias_is_a_symlink_When_detect_runs_Then_it_is_not_an_entry_document()
    {
        var fs = Repo(new() { ["README.md"] = "hi\n" });
        fs.AddFile("/r/AGENTS.md", new MockFileData("# P\n"));
        fs.File.CreateSymbolicLink("/r/CLAUDE.md", "/r/AGENTS.md");
        fs.File.Delete("/r/AGENTS.md");

        var json = Json(Run(fs, "--skill", SkillPath));

        Assert.Equal(JsonValueKind.Null, json.GetProperty("entry").ValueKind);
        Assert.Equal("fresh", Text(json, "mode"));
    }

    [Fact]
    public void Given_a_manifest_with_the_modern_stacks_key_When_detect_runs_Then_it_is_read()
    {
        var json = Json(Detect(new()
        {
            ["AGENTS.md"] = "# P\n",
            ["docs/ai/manifest.json"] = "{\"legislatorVersion\": 25, \"stacks\": [\"dotnet\", \"aurelia\"]}",
        }));

        Assert.Equal("upgrade", Text(json, "mode"));
        Assert.Equal(
            ["dotnet", "aurelia"],
            json.GetProperty("stacks").GetProperty("subscribed").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void Given_a_manifest_that_does_not_parse_When_detect_runs_Then_the_run_fails_loud()
    {
        var fs = Repo(new() { ["AGENTS.md"] = "# P\n", ["docs/ai/manifest.json"] = "{nope" });

        var thrown = Assert.ThrowsAny<Exception>(() => Run(fs, "--skill", SkillPath));

        Assert.Contains("manifest.json", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_the_manifest_is_present_When_detect_runs_Then_it_is_echoed_back_whole()
    {
        var json = Json(Detect(new()
        {
            ["AGENTS.md"] = "# P\n",
            ["docs/ai/manifest.json"] = "{\"legislatorVersion\": 25, \"keep\": [{\"path\": \"docs/x.md\", \"reason\": \"r\"}]}",
        }));

        var keep = json.GetProperty("manifest").GetProperty("keep")[0];
        Assert.Equal("docs/x.md", keep.GetProperty("path").GetString());
        Assert.Equal("r", keep.GetProperty("reason").GetString());
    }

    [Fact]
    public void Given_no_manifest_When_detect_runs_Then_the_manifest_slot_is_null()
    {
        var json = Json(Detect(new() { ["README.md"] = "hi\n" }));

        Assert.Equal(JsonValueKind.Null, json.GetProperty("manifest").ValueKind);
    }

    [Fact]
    public void Given_an_aurelia_dependency_in_package_json_When_detect_runs_Then_aurelia_is_a_candidate()
    {
        var json = Json(Detect(new()
        {
            ["README.md"] = "hi\n",
            ["package.json"] = "{\"dependencies\": {\"aurelia-framework\": \"1.0.0\"}}",
        }));

        Assert.Equal(
            ["aurelia"],
            json.GetProperty("stacks").GetProperty("candidates").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void Given_an_aurelia_project_file_When_detect_runs_Then_aurelia_is_a_candidate()
    {
        var json = Json(Detect(new()
        {
            ["README.md"] = "hi\n",
            ["aurelia_project/aurelia.json"] = "{}\n",
        }));

        Assert.Equal(
            ["aurelia"],
            json.GetProperty("stacks").GetProperty("candidates").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void Given_a_package_json_that_does_not_parse_When_detect_runs_Then_it_is_no_candidate_and_no_crash()
    {
        var json = Json(Detect(new() { ["README.md"] = "hi\n", ["package.json"] = "{nope" }));

        Assert.Empty(json.GetProperty("stacks").GetProperty("candidates").EnumerateArray());
    }

    [Fact]
    public void Given_a_reconstructed_layer_without_a_stacks_directory_When_detect_runs_Then_nothing_is_subscribed()
    {
        var json = Json(Detect(new()
        {
            ["AGENTS.md"] = "# P\n\n@docs/ai/rules/core/okf.md\n",
            ["docs/ai/rules/core/okf.md"] = "x\n",
        }));

        Assert.Equal("upgrade", Text(json, "mode"));
        Assert.Equal(JsonValueKind.True, json.GetProperty("reconstructed").ValueKind);
        Assert.Empty(json.GetProperty("stacks").GetProperty("subscribed").EnumerateArray());
        Assert.Equal(
            ["docs/ai/rules/core/okf.md"],
            json.GetProperty("ownedFilesOld").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void Given_any_repository_When_detect_runs_Then_the_object_keys_are_ordinal_sorted()
    {
        var result = Detect(new() { ["README.md"] = "hi\n" });

        var keys = Json(result).EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equal(keys.Order(StringComparer.Ordinal), keys);
    }
}
