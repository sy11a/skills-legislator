using System.IO.Abstractions;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Legislator.Core.Manifest;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Engine.Apply;
using Legislator.Engine.Detect;
using Legislator.Engine.Runs;

namespace Legislator.Engine.Jobs;

/// <summary>
/// Step 3 whole: the owned set copied in, the retired files taken out, the keep list resolved,
/// the manifest regenerated and the v14 file model wired - and a record of all of it written
/// outside the repository, so verify and report describe the run that happened rather than the
/// tree as it looks afterwards. The one state it refuses is two real entry documents: which of
/// them is canonical is the owner's ruling, and apply stops before its first write rather than
/// choosing.
/// </summary>
public sealed partial class ApplyJob : IJob
{
    private const int UsageError = 2;

    private const int DecisionGateStop = 4;

    private const string StacksFlag = "--stacks";

    private const string KeepAddFlag = "--keep-add";

    private const string KeepRemoveFlag = "--keep-remove";

    private const string RecordFlag = "--record";

    private const string KeepAddSeparator = "::";

    public string Name => "apply";

    public string Usage =>
        $"{Name} --skill <skill-path> {StacksFlag} <a,b> [{KeepAddFlag} <path>::<reason>]* "
        + $"[{KeepRemoveFlag} <path>]* [{RecordFlag} <file>] [--root <dir>]";

    [GeneratedRegex(@"^@(\S+)", RegexOptions.Multiline)]
    private static partial Regex ImportLine();

    public JobResult Run(JobContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var (parsed, error) = SkillArguments.Parse(
            this, ctx, ctx.Fs, ctx.Options, StacksFlag, KeepAddFlag, KeepRemoveFlag, RecordFlag);
        if (parsed is null)
        {
            return error!;
        }

        if (parsed.Value(StacksFlag) is not { } stacksValue)
        {
            return UsageResult();
        }

        var keepAdd = new List<(string Path, string Reason)>();
        foreach (var spec in parsed.All(KeepAddFlag))
        {
            var at = spec.IndexOf(KeepAddSeparator, StringComparison.Ordinal);
            if (at < 0)
            {
                return UsageResult();
            }

            keepAdd.Add((spec[..at].Trim(), spec[(at + KeepAddSeparator.Length)..].Trim()));
        }

        var stacks = stacksValue
            .Split(LegislatorOptions.ListSeparator)
            .Where(s => s.Trim().Length > 0)
            .ToList();
        var fs = ctx.Fs;
        var layout = new RepoLayout(ctx.Options, ctx.Root);
        var detection = Detection.Of(fs, layout, ctx.Options, ctx.Root, parsed.Skill.Version);

        // The stop comes before the first write, not as an exception unwinding past one: the
        // promise is that nothing was written, and the cheapest way to keep a promise like that
        // is to have written nothing yet.
        if (EntryDocument.IsRealAlias(fs, layout, ctx.Options)
            && fs.File.Exists($"{layout.Root}/{ctx.Options.EntryDocument.Value}"))
        {
            return new JobResult(
                DecisionGateStop,
                "",
                $"apply stopped: two canonical candidates: a real {ctx.Options.EntryDocument.Value} and a real "
                + $"{ctx.Options.EntryAlias.Value} both exist - ask the owner which is canonical before anything "
                + "is written (decision gate)\n");
        }

        var version = parsed.Skill.Version ?? "0";
        var pre = new JsonObject
        {
            [RunRecord.Step4Field] = Snapshot(fs, parsed, ctx.Options, layout),
            [RunRecord.ImportsField] = RunRecord.Array(Imports(fs, layout, detection.Entry)),
            [RunRecord.EntryKey] = detection.Entry,
        };

        var sources = OwnedSet.Of(fs, parsed.Skill, layout, ctx.Options, stacks);
        var (created, overwritten, unchanged) = Copy(fs, layout, sources);
        var deleted = Retire(fs, layout, sources, detection.OwnedFilesOld);
        var keep = KeepList.Resolve(
            Carried(detection.Manifest),
            keepAdd,
            parsed.All(KeepRemoveFlag),
            sources.Keys.ToHashSet(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal) { layout.Relative(layout.Baseline) },
            path => fs.File.Exists($"{layout.Root}/{path}") || fs.Directory.Exists($"{layout.Root}/{path}"));

        var text = ManifestText.Render(version, stacks, keep.Final, [.. sources.Keys]);
        var existed = fs.File.Exists(layout.Manifest);
        var changed = !existed || fs.File.ReadAllText(layout.Manifest) != text;
        if (changed)
        {
            fs.Directory.CreateDirectory(layout.Ai);
            fs.File.WriteAllText(layout.Manifest, text);
        }

        var events = FileModel.Wire(fs, ctx.Proc, ctx.Options, layout);
        var recordPath = RecordPath.Of(fs, ctx.Options, ctx.Root, parsed.Value(RecordFlag));
        RunRecord.Write(fs, ctx.Root, recordPath, new JsonObject
        {
            [RunRecord.EntryKey] = detection.Entry,
            [RunRecord.FileModelKey] = RunRecord.Array(events),
            [RunRecord.KeepKey] = new JsonObject
            {
                [RunRecord.AddedField] = Entries(keep.Added),
                [RunRecord.FinalField] = Entries(keep.Final),
                [RunRecord.RefusedField] = Entries(keep.Refused),
                [RunRecord.RemovedField] = Paths(keep.Removed),
            },
            [RunRecord.ManifestKey] = new JsonObject
            {
                [RunRecord.ExistedField] = existed,
                [RunRecord.WrittenField] = changed,
            },
            [RunRecord.ModeKey] = detection.Mode,
            [RunRecord.OwnedKey] = new JsonObject
            {
                [RunRecord.CreatedField] = RunRecord.Array(created),
                [RunRecord.DeletedField] = RunRecord.Array(deleted),
                [RunRecord.OverwrittenField] = RunRecord.Array(overwritten),
                [RunRecord.UnchangedField] = RunRecord.Array(unchanged),
            },
            [RunRecord.PreKey] = pre,
            [RunRecord.ReconstructedKey] = detection.Reconstructed,
            [RunRecord.RootKey] = ctx.Root,
            [RunRecord.StacksKey] = RunRecord.Array(stacks),
            [RunRecord.VersionKey] = version,
            [RunRecord.VersionOldKey] = ManifestFile.Scalar(detection.Manifest, ManifestFile.VersionKey),
        });

        var stdout = new StringBuilder();
        stdout.Append($"apply: {detection.Mode} mode, constitution v{version}, stacks [{string.Join(", ", stacks.Select(s => $"\"{s}\""))}]\n");
        stdout.Append($"  owned: {created.Count} created, {overwritten.Count} overwritten, {unchanged.Count} unchanged, {deleted.Count} deleted\n");
        stdout.Append($"  keep: {keep.Added.Count} added, {keep.Removed.Count} removed, {keep.Refused.Count} refused\n");
        foreach (var ev in events)
        {
            stdout.Append($"  file model: {ev}\n");
        }

        stdout.Append($"run record: {recordPath}\n");
        return new JobResult(0, stdout.ToString(), "");
    }

    /// <summary>The copies (Step 3.1-3.2, 3.4), classified by what they did to the file that was there.</summary>
    private static (List<string> Created, List<string> Overwritten, List<string> Unchanged) Copy(
        IFileSystem fs, RepoLayout layout, IReadOnlyDictionary<string, string> sources)
    {
        List<string> created = [], overwritten = [], unchanged = [];
        foreach (var (relative, source) in sources.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var target = $"{layout.Root}/{relative}";
            var data = fs.File.ReadAllBytes(source);
            if (!fs.File.Exists(target))
            {
                created.Add(relative);
            }
            else if (!fs.File.ReadAllBytes(target).SequenceEqual(data))
            {
                overwritten.Add(relative);
            }
            else
            {
                unchanged.Add(relative);
                continue;
            }

            fs.Directory.CreateDirectory(target[..target.LastIndexOf('/')]);
            fs.File.WriteAllBytes(target, data);
        }

        return (created, overwritten, unchanged);
    }

    /// <summary>Step 3.5: an owned file the package no longer delivers leaves, and a stack directory it emptied leaves with it.</summary>
    private static List<string> Retire(
        IFileSystem fs,
        RepoLayout layout,
        IReadOnlyDictionary<string, string> sources,
        IReadOnlyList<string> ownedOld)
    {
        var deleted = new List<string>();
        foreach (var relative in ownedOld.Order(StringComparer.Ordinal))
        {
            var path = $"{layout.Root}/{relative}";
            if (sources.ContainsKey(relative) || !fs.File.Exists(path))
            {
                continue;
            }

            fs.File.Delete(path);
            deleted.Add(relative);

            var parent = path[..path.LastIndexOf('/')];
            if (parent[..parent.LastIndexOf('/')] == layout.RuleStacks
                && fs.Directory.Exists(parent)
                && !fs.Directory.EnumerateFileSystemEntries(parent).Any())
            {
                fs.Directory.Delete(parent);
            }
        }

        return deleted;
    }

    private static IEnumerable<KeepEntry> Carried(JsonNode? manifest) =>
        ManifestFile.Objects(manifest, ManifestFile.KeepKey)
            .Select(e => (
                Path: e[ManifestFile.PathField]?.GetValue<string>(),
                Reason: e[ManifestFile.ReasonField]?.GetValue<string>()))
            .Where(e => e.Path is not null)
            .Select(e => new KeepEntry(e.Path!, e.Reason ?? ""));

    private static IReadOnlyList<string> Imports(IFileSystem fs, RepoLayout layout, string? entry) =>
        entry is null
            ? []
            : [.. ImportLine().Matches(fs.File.ReadAllText($"{layout.Root}/{entry}"))
                .Cast<Match>().Select(m => m.Groups[1].Value)];

    private static JsonObject Snapshot(
        IFileSystem fs, SkillArguments parsed, LegislatorOptions options, RepoLayout layout)
    {
        var snapshot = new JsonObject();
        foreach (var (target, present) in Step4Targets.Snapshot(fs, parsed.Skill, options, layout))
        {
            snapshot[target] = present;
        }

        return snapshot;
    }

    private static JsonArray Entries(IReadOnlyList<KeepEntry> entries)
    {
        var array = new JsonArray();
        foreach (var entry in entries)
        {
            array.Add((JsonNode)new JsonObject
            {
                [RunRecord.PathField] = entry.Path,
                [RunRecord.ReasonField] = entry.Reason,
            });
        }

        return array;
    }

    private static JsonArray Paths(IReadOnlyList<string> paths)
    {
        var array = new JsonArray();
        foreach (var path in paths)
        {
            array.Add((JsonNode)new JsonObject { [RunRecord.PathField] = path });
        }

        return array;
    }

    private JobResult UsageResult() => new(UsageError, "", $"usage: legislator {Usage}\n");
}
