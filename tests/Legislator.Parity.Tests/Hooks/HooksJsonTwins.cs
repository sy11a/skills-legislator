using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Legislator.Parity.Tests.Hooks;

/// <summary>
/// The named twins of the `hooks.json` well-formedness assertions (R-8206). These read the
/// shipped file rather than a fixture, because the claim IS about the delivered registration:
/// a matcher naming a tool Claude Code does not have registers a hook that never fires. The
/// three assertions bound to the file naming a Python script - the two launcher checks and the
/// `.py` command shape - are NOT twinned here: T-13 rewrites that line to the binary and owns
/// their re-cut, and a twin written now would be born to be deleted (ruling 2026-09-03).
/// </summary>
public sealed class HooksJsonTwins
{
    /// <summary>The tools a Claude Code matcher may name - the ruler's `KNOWN_TOOLS`, which is the same closed set on both arms.</summary>
    private static readonly string[] KnownTools =
    [
        "Edit", "Write", "MultiEdit", "NotebookEdit", "Bash", "Read",
        "Glob", "Grep", "WebFetch", "WebSearch", "Task", "NotebookRead",
    ];

    private static JsonNode Read()
    {
        var path = Path.Combine(Labels.RepoRoot(), "plugin", "hooks", "hooks.json");

        return JsonNode.Parse(File.ReadAllText(path))
            ?? throw new JsonException($"{path} parsed to nothing");
    }

    [Fact]
    [Parity("hooks", "hooks.json parses as JSON")]
    public void Hooks_json_parses_as_json()
    {
        Assert.NotNull(Read());
    }

    [Fact]
    [Parity("hooks", "hooks.json has at least one event")]
    public void Hooks_json_has_at_least_one_event()
    {
        var events = Read()["hooks"]?.AsObject();

        Assert.NotNull(events);
        Assert.NotEmpty(events);
    }

    [Fact]
    [Parity("hooks", "{} entries form a list")]
    public void Every_events_entries_form_a_list()
    {
        foreach (var (name, entries) in Read()["hooks"]!.AsObject())
        {
            Assert.True(entries is JsonArray, $"{name} entries are not a list");
        }
    }

    [Fact]
    [Parity("hooks", "{} matcher names real tools ({})")]
    public void Every_matcher_names_real_tools()
    {
        foreach (var (name, entries) in Read()["hooks"]!.AsObject())
        {
            foreach (var entry in entries!.AsArray())
            {
                var matcher = entry?["matcher"]?.GetValue<string>();
                if (matcher is null)
                {
                    continue;
                }

                var unknown = matcher.Split('|').Where(t => !KnownTools.Contains(t, StringComparer.Ordinal)).ToList();
                Assert.True(unknown.Count == 0, $"{name} matcher '{matcher}' names {string.Join(", ", unknown)}");
            }
        }
    }
}
