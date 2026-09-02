using System.Buffers;
using System.IO.Abstractions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Legislator.Core.Manifest;
using Legislator.Core.Repo;
using Legislator.Engine.Detect;

namespace Legislator.Engine.Jobs;

/// <summary>
/// Step 1's decision tree and Step 2's signals, as data: which mode a run over this repository
/// would be, which entry document it would read, what it is subscribed to and what it already
/// owns. It writes nothing - the answer is JSON on stdout, and the deciding is the caller's.
/// </summary>
public sealed class DetectJob : IJob
{
    private const string Fresh = "fresh";

    private const string Migration = "migration";

    private const string Upgrade = "upgrade";

    public string Name => "detect";

    public string Usage => $"{Name} --skill <skill-path> [--root <dir>]";

    public JobResult Run(JobContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var (parsed, error) = SkillArguments.Parse(this, ctx, ctx.Fs, ctx.Options);
        if (parsed is null)
        {
            return error!;
        }

        var fs = ctx.Fs;
        var layout = new RepoLayout(ctx.Options, ctx.Root);
        var manifest = ManifestFile.Read(fs, layout);
        var entry = EntryDocument.Of(fs, layout, ctx.Options);

        string mode;
        var reconstructed = false;
        IReadOnlyList<string> subscribed;
        IReadOnlyList<string> ownedOld;
        if (manifest is not null)
        {
            mode = Upgrade;
            // `stacks` present wins even when empty: an edition that wrote the modern key has
            // already said what it is subscribed to, and falling back would resurrect a legacy
            // list it deliberately emptied.
            subscribed = ManifestFile.Declares(manifest, ManifestFile.StacksKey)
                ? ManifestFile.Strings(manifest, ManifestFile.StacksKey)
                : ManifestFile.Strings(manifest, ManifestFile.ProfilesKey);
            ownedOld = ManifestFile.Strings(manifest, ManifestFile.OwnedFilesKey);
        }
        else if (entry is not null && Imports(fs, layout, entry))
        {
            // The manifest is gone but the layer is plainly installed: the owned set and the
            // subscriptions are read back off disk rather than assumed absent, so an upgrade
            // does not silently re-scaffold over a repository that already has a constitution.
            mode = Upgrade;
            reconstructed = true;
            subscribed = SubscribedOnDisk(fs, layout);
            ownedOld = OwnedOnDisk(fs, layout);
        }
        else
        {
            mode = entry is not null ? Migration : Fresh;
            subscribed = [];
            ownedOld = [];
        }

        return new JobResult(0, Render(
            mode, entry, reconstructed, manifest, subscribed,
            StackCandidates.Of(fs, ctx.Root, ctx.Options), ownedOld, parsed.Skill.Version), "");
    }

    private static bool Imports(IFileSystem fs, RepoLayout layout, string entry) =>
        fs.File.ReadAllText($"{layout.Root}/{entry}").Contains($"@{layout.LegislationImport}", StringComparison.Ordinal);

    private static IReadOnlyList<string> SubscribedOnDisk(IFileSystem fs, RepoLayout layout) =>
        fs.Directory.Exists(layout.RuleStacks)
            ? [.. fs.Directory.EnumerateDirectories(layout.RuleStacks)
                .Select(d => d.Replace('\\', '/').TrimEnd('/'))
                .Select(d => d[(d.LastIndexOf('/') + 1)..])
                .Order(StringComparer.Ordinal)]
            : [];

    private static List<string> OwnedOnDisk(IFileSystem fs, RepoLayout layout)
    {
        var owned = new List<string>();
        if (fs.Directory.Exists(layout.Rules))
        {
            owned.AddRange(fs.Directory
                .EnumerateFiles(layout.Rules, "*", SearchOption.AllDirectories)
                .Select(f => layout.Relative(f)));
        }

        foreach (var extra in new[] { layout.Opencode, layout.Engine })
        {
            if (fs.File.Exists(extra))
            {
                owned.Add(layout.Relative(extra));
            }
        }

        owned.Sort(StringComparer.Ordinal);
        return owned;
    }

    /// <summary>The answer as JSON, keys ordinal-sorted and written by hand so the shape stays AOT-trivial and the manifest is echoed back exactly as the repository wrote it.</summary>
    private static string Render(
        string mode,
        string? entry,
        bool reconstructed,
        JsonNode? manifest,
        IReadOnlyList<string> subscribed,
        IReadOnlyList<string> candidates,
        IReadOnlyList<string> ownedOld,
        string? version)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true, IndentSize = 1 }))
        {
            json.WriteStartObject();
            if (entry is null)
            {
                json.WriteNull("entry");
            }
            else
            {
                json.WriteString("entry", entry);
            }

            json.WritePropertyName("manifest");
            if (manifest is null)
            {
                json.WriteNullValue();
            }
            else
            {
                manifest.WriteTo(json);
            }

            json.WriteString("mode", mode);
            WriteArray(json, "ownedFilesOld", ownedOld);
            json.WriteBoolean("reconstructed", reconstructed);
            json.WriteStartObject("stacks");
            WriteArray(json, "candidates", candidates);
            WriteArray(json, "subscribed", subscribed);
            json.WriteEndObject();
            json.WriteString("version", version);
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan) + "\n";
    }

    private static void WriteArray(Utf8JsonWriter json, string name, IReadOnlyList<string> values)
    {
        json.WriteStartArray(name);
        foreach (var value in values)
        {
            json.WriteStringValue(value);
        }

        json.WriteEndArray();
    }
}
