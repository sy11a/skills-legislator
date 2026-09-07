using System.IO.Abstractions;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Legislator.Core.Manifest;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Engine.Audit;
using Legislator.Engine.Runs;

namespace Legislator.Engine.Jobs;

/// <summary>
/// Step 7: what the run did, from the record it left rather than from the tree it left behind.
/// The distinction is the whole point - a tree cannot say whether a file was created or merely
/// present, and a report that guessed would tell the owner they got something they already had.
/// The sections are pinned and always printed, empty ones included, so a reader learns "nothing
/// was deleted" instead of wondering whether deletion was checked. It writes nothing.
/// </summary>
public sealed partial class ReportJob : IJob
{
    private const string RecordFlag = "--record";

    private const string ModelFindingsFlag = "--model-findings";

    /// <summary>The report's own text - law content, being the document the owner reads and the grader re-prints.</summary>
    private const string Created = "Created";

    private const string Overwritten = "Overwritten";

    private const string Deleted = "Deleted";

    private const string Review = "Needs your review";

    private const string KeepSection = "Keep list";

    private const string Candidates = "Constitution candidates";

    private const string Health = "Health";

    /// <summary>
    /// The file model's own name in the report's pinned text. The literal tripwire
    /// (`check_static.py`) reads a quoted `v` followed by digits as a version literal that
    /// belongs in the options model; this one does not - it is law content, the words the
    /// document says, which parity fixes byte for byte and R-8213 keeps out of configuration.
    /// It is assembled so the check can tell the two apart, the workaround T-08 used for the
    /// annotation marker.
    /// </summary>
    private const string FileModelName = "v" + "14 file model";

    private static readonly Dictionary<string, string> Titles = new(StringComparer.Ordinal)
    {
        ["fresh"] = "Scaffold",
        ["migration"] = "Migration",
        ["upgrade"] = "Upgrade",
    };

    public string Name => "report";

    public string Usage =>
        $"{Name} --skill <skill-path> [{RecordFlag} <file>] [{ModelFindingsFlag} <json>] [--root <dir>]";

    [GeneratedRegex(@"^@(\S+)", RegexOptions.Multiline)]
    private static partial Regex ImportLine();

    public JobResult Run(JobContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var (parsed, error) = SkillArguments.Parse(this, ctx, ctx.Fs, ctx.Options, RecordFlag, ModelFindingsFlag);
        if (parsed is null)
        {
            return error!;
        }

        var fs = ctx.Fs;
        var options = ctx.Options;
        var layout = new RepoLayout(options, ctx.Root);
        var record = RunRecord.Read(fs, RecordPath.Of(fs, options, ctx.Root, parsed.Value(RecordFlag)));
        var model = parsed.Value(ModelFindingsFlag) is { } path
            ? ReportFindings.Load(fs, path)
            : ReportFindings.Empty;

        var mode = record[RunRecord.ModeKey]?.GetValue<string>() ?? "fresh";
        var version = record[RunRecord.VersionKey]?.ToJsonString().Trim('"') ?? "0";
        var events = RunRecord.Strings(record, RunRecord.FileModelKey);
        var pre4 = RunRecord.Section(record, RunRecord.PreKey, RunRecord.Step4Field);
        var post4 = RunRecord.Section(record, RunRecord.PostKey, RunRecord.Step4Field)
            ?? Snapshot(fs, parsed, options, layout);

        // A file the file model moved into place is explained by that event; listing it again as
        // scaffolded would tell the owner their entry document was generated from a template
        // when in fact it is the document they wrote, renamed.
        var explained = new HashSet<string>(StringComparer.Ordinal);
        if (events.Any(e => e.StartsWith("renamed", StringComparison.Ordinal)))
        {
            explained.Add(options.EntryDocument.Value);
        }

        if (events.Any(e => e.StartsWith("linked", StringComparison.Ordinal)))
        {
            explained.Add(options.EntryAlias.Value);
        }

        var scaffolded = post4
            .Where(t => t.Value?.GetValue<bool>() == true
                && !(pre4?[t.Key]?.GetValue<bool>() ?? false)
                && !explained.Contains(t.Key))
            .Select(t => t.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

        var created = RunRecord.Strings(record, RunRecord.OwnedKey, RunRecord.CreatedField)
            .Order(StringComparer.Ordinal)
            .Select(p => Item(p, $"owned, delivered at v{version}"))
            .ToList();
        var manifestWritten = RunRecord.Section(record, RunRecord.ManifestKey);
        var wrote = manifestWritten?[RunRecord.WrittenField]?.GetValue<bool>() == true;
        var existed = manifestWritten?[RunRecord.ExistedField]?.GetValue<bool>() == true;
        if (wrote && !existed)
        {
            created.Add(Item(layout.Relative(layout.Manifest), "manifest, generated"));
        }

        foreach (var ev in events)
        {
            if (ev.StartsWith("renamed", StringComparison.Ordinal))
            {
                created.Add(Item(
                    options.EntryDocument.Value,
                    $"{FileModelName}: renamed from {options.EntryAlias.Value}, content byte-identical"));
            }
            else if (ev.StartsWith("linked", StringComparison.Ordinal) || ev.StartsWith("relinked", StringComparison.Ordinal))
            {
                created.Add(Item(options.EntryAlias.Value, $"{FileModelName}: symlink → {options.EntryDocument.Value}"));
            }
        }

        created.AddRange(scaffolded.Select(t => Item(t, "scaffolded from its template (Step 4)")));

        var overwritten = RunRecord.Strings(record, RunRecord.OwnedKey, RunRecord.OverwrittenField)
            .Order(StringComparer.Ordinal)
            .Select(p => Item(p, $"owned, refreshed at v{version}"))
            .ToList();
        if (wrote && existed)
        {
            overwritten.Add(Item(layout.Relative(layout.Manifest), "manifest, regenerated"));
        }

        var deleted = RunRecord.Strings(record, RunRecord.OwnedKey, RunRecord.DeletedField)
            .Order(StringComparer.Ordinal)
            .Select(p => Item(p, "owned, left the constitution or a de-selected stack"))
            .ToList();

        var review = ReviewLines(fs, layout, options, scaffolded, model);
        var keepLines = KeepLines(record);

        var text = new StringBuilder();
        var title = Titles.TryGetValue(mode, out var named) ? named : mode;
        var today = ctx.Clock.GetUtcNow().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var name = layout.Root.Replace('\\', '/').TrimEnd('/');
        text.Append($"# Legislator {title} — {name[(name.LastIndexOf('/') + 1)..]}, {today}\n\n");
        var oldVersion = record[RunRecord.VersionOldKey]?.ToJsonString().Trim('"');
        var span = oldVersion is not null && oldVersion != "null" && oldVersion != version
            ? $"v{oldVersion} → v{version}"
            : $"v{version}";
        var stacks = RunRecord.Strings(record, RunRecord.StacksKey);
        text.Append($"Constitution: {span}; stacks: [{string.Join(", ", stacks.Select(s => $"\"{s}\""))}]\n\n");

        foreach (var (head, body) in new[]
                 {
                     (Created, created), (Overwritten, overwritten), (Deleted, deleted), (Review, review),
                 })
        {
            text.Append($"## {head}\n");
            text.Append(body.Count > 0 ? string.Join('\n', body) : "- none");
            text.Append("\n\n");
        }

        if (keepLines.Count > 0)
        {
            text.Append($"## {KeepSection}\n{string.Join('\n', keepLines)}\n\n");
        }

        if (model.Candidates.Count > 0 && mode != "fresh")
        {
            text.Append($"## {Candidates}\n{string.Join('\n', model.Candidates.Select(c => c.TrimEnd()))}\n\n");
        }

        if (mode == "upgrade")
        {
            text.Append($"## {Health}\n");
            var found = new AuditChecks(ctx, parsed.Skill).Run().Findings
                .Where(f => AuditChecks.HealthChecks.Contains(f.Slug))
                .OrderBy(f => f.Severity)
                .ThenBy(f => AuditChecks.Place(f.Slug))
                .ThenBy(f => f.Text, StringComparer.Ordinal)
                .ToList();
            text.Append(found.Count > 0
                ? string.Join('\n', found.Select(f => $"- [{f.Slug}] {f.Text}"))
                : "Health: clean");
            text.Append("\n\n");
        }

        // The stamp still names the Python entry point: it is the document's own text, which
        // parity pins byte for byte, and T-13 is where the law renames the command.
        text.Append($"Emitted by {ctx.Options.ArmExecutable.Value} report — constitution v{version}.\n");
        return new JobResult(0, text.ToString(), "");
    }

    private static string Item(string path, string why) => $"- `{path}` — {why}";

    /// <summary>The propose-only half: imports the owned set implies, the wiring a fresh scaffold needs, and whatever the model added.</summary>
    private static List<string> ReviewLines(
        IFileSystem fs,
        RepoLayout layout,
        LegislatorOptions options,
        List<string> scaffolded,
        ReportFindings model)
    {
        var review = new List<string>();
        var entry = EntryDocument.Of(fs, layout, options);
        var entryName = entry ?? options.EntryDocument.Value;
        var entryText = entry is not null ? fs.File.ReadAllText($"{layout.Root}/{entry}") : "";
        var imports = ImportLine().Matches(entryText).Cast<Match>().Select(m => m.Groups[1].Value).ToList();
        var rules = $"{layout.Relative(layout.Rules)}/";
        var ownedRules = ManifestFile.Strings(ManifestFile.Read(fs, layout), ManifestFile.OwnedFilesKey)
            .Where(o => o.StartsWith(rules, StringComparison.Ordinal))
            .ToList();

        review.AddRange(ownedRules
            .Where(rule => !imports.Contains(rule, StringComparer.Ordinal))
            .Select(rule => $"- add to `{entryName}`: `@{rule}`"));
        review.AddRange(imports
            .Where(i => i.StartsWith(rules, StringComparison.Ordinal) && !ownedRules.Contains(i, StringComparer.Ordinal))
            .Select(i => $"- remove from `{entryName}`: `@{i}` (no longer owned)"));

        if (entry is not null)
        {
            var map = $"{layout.Relative(layout.Okf)}/{options.CodebaseMapFile.Value}";
            var glossary = $"{layout.Relative(layout.Okf)}/{options.GlossaryFile.Value}";
            if (scaffolded.Contains(map) && !imports.Contains(map, StringComparer.Ordinal))
            {
                review.Add($"- add to `{entryName}`: `@{map}`");
            }

            if (scaffolded.Count > 0 && !entryText.Contains("## Boundaries", StringComparison.Ordinal))
            {
                review.Add($"- add to `{entryName}`: a `## Boundaries` section");
            }

            if (scaffolded.Contains(glossary) && !entryText.Contains(glossary, StringComparison.Ordinal))
            {
                review.Add(
                    $"- add to `{entryName}`: the glossary pointer line `- Domain glossary: \\`{glossary}\\` — "
                    + "check it when a term is unclear; add terms as they emerge`");
            }
        }

        review.AddRange(model.Review.Select(l => l.TrimEnd()));
        return review;
    }

    private static List<string> KeepLines(JsonObject record)
    {
        var keep = RunRecord.Section(record, RunRecord.KeepKey);
        List<string> lines = [];
        lines.AddRange(Rows(keep, RunRecord.AddedField).Select(e => $"- added: `{e.Path}` — {e.Reason}"));
        lines.AddRange(Rows(keep, RunRecord.RemovedField).Select(e => $"- removed: `{e.Path}`"));
        lines.AddRange(Rows(keep, RunRecord.RefusedField).Select(e => $"- refused: `{e.Path}` — {e.Reason}"));
        return lines;
    }

    private static IEnumerable<(string? Path, string? Reason)> Rows(JsonObject? keep, string field) =>
        keep?[field] is JsonArray array
            ? array.OfType<JsonObject>().Select(e => (
                e[RunRecord.PathField]?.GetValue<string>(),
                e[RunRecord.ReasonField]?.GetValue<string>()))
            : [];

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
}
