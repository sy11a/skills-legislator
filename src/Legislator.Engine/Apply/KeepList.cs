namespace Legislator.Engine.Apply;

/// <summary>One promise the manifest carries: a path the engine will not touch, and the reason the owner gave for it.</summary>
public sealed record KeepEntry(string Path, string Reason);

/// <summary>What one resolution of the keep list produced: the list itself, and the three deltas the run record and the report print.</summary>
public sealed record KeepOutcome(
    IReadOnlyList<KeepEntry> Final,
    IReadOnlyList<KeepEntry> Added,
    IReadOnlyList<string> Removed,
    IReadOnlyList<KeepEntry> Refused);

/// <summary>
/// The keep rules (Step 3.6). A keep entry is a promise the engine can actually make, so the
/// three paths it cannot promise about are refused by name rather than accepted and quietly
/// broken: an owned file is replaced every run, a generated artifact is rewritten wholesale,
/// and a path that is not there cannot be left alone. Refusals are recorded, never silent -
/// the owner asked for something and is owed the reason it did not happen.
/// </summary>
public static class KeepList
{
    public const string OwnedRefusal =
        "the path is an owned file (machine-managed, replaced every run - not keepable)";

    public const string GeneratedRefusal =
        "the path is a generated artifact (rewritten wholesale by the engine - a keep promise on it is unkeepable)";

    public const string MissingRefusal = "the path does not exist in the repo";

    public static KeepOutcome Resolve(
        IEnumerable<KeepEntry> carried,
        IEnumerable<(string Path, string Reason)> add,
        IEnumerable<string> remove,
        IReadOnlySet<string> owned,
        IReadOnlySet<string> generated,
        Func<string, bool> exists)
    {
        ArgumentNullException.ThrowIfNull(carried);
        ArgumentNullException.ThrowIfNull(add);
        ArgumentNullException.ThrowIfNull(remove);
        ArgumentNullException.ThrowIfNull(owned);
        ArgumentNullException.ThrowIfNull(generated);
        ArgumentNullException.ThrowIfNull(exists);

        var keep = carried.ToList();
        var added = new List<KeepEntry>();
        var refused = new List<KeepEntry>();
        var removed = new List<string>();

        foreach (var (path, reason) in add)
        {
            var refusal =
                owned.Contains(path) ? OwnedRefusal
                : generated.Contains(path) ? GeneratedRefusal
                : !exists(path) ? MissingRefusal
                : null;
            if (refusal is not null)
            {
                refused.Add(new KeepEntry(path, refusal));
                continue;
            }

            // By path, not by entry: asking again with a new reason is a correction of the
            // promise, not a second promise about the same file.
            keep.RemoveAll(k => string.Equals(k.Path, path, StringComparison.Ordinal));
            var entry = new KeepEntry(path, reason);
            keep.Add(entry);
            added.Add(entry);
        }

        foreach (var path in remove)
        {
            if (keep.RemoveAll(k => string.Equals(k.Path, path, StringComparison.Ordinal)) > 0)
            {
                removed.Add(path);
            }
        }

        return new KeepOutcome(
            [.. keep.OrderBy(k => k.Path, StringComparer.Ordinal)],
            added,
            removed,
            refused);
    }
}
