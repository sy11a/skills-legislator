using System.IO.Abstractions;
using System.Text.Json.Nodes;
using Legislator.Core.Manifest;
using Legislator.Core.Options;
using Legislator.Core.Repo;

namespace Legislator.Engine.Detect;

/// <summary>
/// What a run over this repository would be: Step 1's decision tree and Step 2's signals, as
/// data. Detect renders it as JSON and stops there; apply acts on it. It lives apart from
/// either because both must read the same tree the same way - an apply that decided its own
/// mode could disagree with the detect the owner ran a moment earlier and looked at.
/// </summary>
public sealed record Detection(
    string Mode,
    string? Entry,
    bool Reconstructed,
    JsonNode? Manifest,
    IReadOnlyList<string> Subscribed,
    IReadOnlyList<string> Candidates,
    IReadOnlyList<string> OwnedFilesOld,
    string? Version)
{
    public const string Fresh = "fresh";

    public const string Migration = "migration";

    public const string Upgrade = "upgrade";

    public static Detection Of(
        IFileSystem fs, RepoLayout layout, LegislatorOptions options, string root, string? version)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(options);

        var manifest = ManifestFile.Read(fs, layout);
        var entry = EntryDocument.Of(fs, layout, options);

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

        return new Detection(
            mode, entry, reconstructed, manifest, subscribed,
            StackCandidates.Of(fs, root, options), ownedOld, version);
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

        foreach (var extra in new[] { layout.Opencode })
        {
            if (fs.File.Exists(extra))
            {
                owned.Add(layout.Relative(extra));
            }
        }

        owned.Sort(StringComparer.Ordinal);
        return owned;
    }
}
