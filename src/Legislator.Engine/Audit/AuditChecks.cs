using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Legislator.Core.Manifest;
using Legislator.Core.Options;
using Legislator.Core.Repo;
using Legislator.Core.Skill;
using Legislator.Engine.Anchors;
using Legislator.Engine.Jobs;
using Legislator.Engine.Okf;
using Legislator.Engine.Sdd;
using Legislator.Engine.Apply;

namespace Legislator.Engine.Audit;

/// <summary>
/// The mechanical half of the audit - every check `SKILL.md` § Audit defines except 11 and 12,
/// which need a reader's judgement and arrive through the model-findings channel. The order of
/// the methods is the order of the numbered law, so a check and its definition are found the
/// same way. Reads everything, writes nothing (ADR-0003).
/// </summary>
public sealed partial class AuditChecks(JobContext job, SkillPackage skill)
{
    /// <summary>The pinned print order of the checks - law content, not configuration: these are the names `SKILL.md` gives them.</summary>
    public static IReadOnlyList<string> Order { get; } =
    [
        "imports-resolve", "unresolved-placeholders", "owned-integrity", "staleness",
        "okf-index-links", "codebase-map", "orphan-docs", "journal-recency",
        "foreign-structures", "keep-list", "project-rules", "stray-rulebooks",
        "glossary-vitality", "skill-bindings", "okf-anchors", "legacy-home-violation",
        "okf-sync-debt", "tracker-drift", "case-collisions", "arm-integrity",
    ];

    /// <summary>A check's place in the pinned order, and the far end for anything the model named that the order does not - an unknown slug prints last rather than crashing the report.</summary>
    public static int Place(string slug) => places.TryGetValue(slug, out var at) ? at : Order.Count;

    private static readonly Dictionary<string, int> places =
        Order.Select((slug, at) => (slug, at)).ToDictionary(p => p.slug, p => p.at, StringComparer.Ordinal);

    /// <summary>
    /// The checks the Step-7 report's Health section runs - the first six of the pinned order,
    /// derived from it rather than restated, so a renamed check cannot leave the report asking
    /// for a slug the audit no longer raises.
    /// </summary>
    public static IReadOnlySet<string> HealthChecks { get; } =
        new HashSet<string>(Order.Take(6), StringComparer.Ordinal);

    /// <summary>The two checks no engine can perform - they are judgements about meaning, and the model supplies them.</summary>
    public static IReadOnlySet<string> ModelChecks { get; } =
        new HashSet<string>(StringComparer.Ordinal) { "project-rules", "stray-rulebooks" };

    private readonly IFileSystem fs = job.Fs;

    private readonly LegislatorOptions options = job.Options;

    private readonly RepoLayout layout = new(job.Options, job.Root);

    [GeneratedRegex(@"\]\(([^)#\s]+)\)")]
    private static partial Regex MarkdownLink();

    [GeneratedRegex(@"^@(\S+)", RegexOptions.Multiline)]
    private static partial Regex ImportLine();

    [GeneratedRegex(@"^\|\s*`([^`]+)`", RegexOptions.Multiline)]
    private static partial Regex MapRow();

    [GeneratedRegex(@"`([a-z][a-z0-9-]{2,})`")]
    private static partial Regex SkillNameToken();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}")]
    private static partial Regex LeadingDate();

    [GeneratedRegex(@"\d{4}-\d{2}-\d{2}")]
    private static partial Regex AnyDate();

    [GeneratedRegex(@"^\|[\s|-]+$")]
    private static partial Regex RuleRow();

    [GeneratedRegex(@"^(?:#{2,}\s+|\s*[-*]\s+)\**\s*([A-Z]{2,}-\d+)")]
    private static partial Regex WorkItem();

    [GeneratedRegex(@"^\s*<!--.*mirror generated.*-->\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex MirrorMarker();

    public AuditResult Run()
    {
        var result = new AuditResult();
        var entry = EntryDocument.Of(fs, layout, options);
        var read = ManifestFile.TryRead(fs, layout);
        var manifest = read.Node;
        var everyDocument = AllMarkdown(entry);

        result.AddRange(Check01ImportsResolve(entry));
        result.AddRange(Check02UnresolvedPlaceholders(entry));
        result.AddRange(Check03OwnedIntegrity(read));
        result.AddRange(Check04Staleness(manifest));
        result.AddRange(Check05OkfIndexLinks());
        result.AddRange(Check06CodebaseMap());
        result.AddRange(Check07OrphanDocs(everyDocument));
        result.AddRange(Check08JournalRecency());
        result.AddRange(Check09ForeignStructures(manifest, entry));
        result.AddRange(Check10KeepList(manifest, everyDocument));
        result.AddRange(Check13GlossaryVitality());
        result.AddRange(Check14SkillBindings());
        result.AddRange(Check16LegacyHomeViolation());
        result.AddRange(Check15OkfAnchorsAndSyncDebt());
        result.AddRange(Check18TrackerDrift(entry));
        result.AddRange(Check19CaseCollisions(read));
        result.AddRange(Check20ArmIntegrity());
        return result;
    }

    private IEnumerable<AuditFinding> Check01ImportsResolve(string? entry)
    {
        if (entry is null)
        {
            yield break;
        }

        foreach (var match in ImportLine().Matches(Read($"{layout.Root}/{entry}")).Cast<Match>())
        {
            var target = match.Groups[1].Value;
            if (!Exists($"{layout.Root}/{target}"))
            {
                yield return new(Severity.Critical, "imports-resolve",
                    $"{entry}: `@{target}` does not resolve → the file does not exist; remove the import line or restore it");
            }
        }
    }

    private IEnumerable<AuditFinding> Check02UnresolvedPlaceholders(string? entry)
    {
        var template = $"{layout.Adr}/{options.AdrTemplateFile.Value}";
        foreach (var file in Scanned(entry).Where(f => f != template))
        {
            var lineNumber = 0;
            foreach (var line in Prose.ProseOnly(Read(file)).Split('\n'))
            {
                lineNumber++;
                foreach (var match in Prose.Placeholder().Matches(line).Cast<Match>())
                {
                    yield return new(Severity.Critical, "unresolved-placeholders",
                        $"{layout.Relative(file)}:{lineNumber}: bare `{match.Value}` token left unfilled → fill it in or regenerate the file from the template");
                }
            }
        }
    }

    private IEnumerable<AuditFinding> Check03OwnedIntegrity(ManifestRead read)
    {
        if (read is { Present: true, Parsed: false })
        {
            yield return new(Severity.Critical, "owned-integrity",
                $"{layout.Relative(layout.Manifest)}: does not parse as JSON → re-run /legislator to regenerate it");
        }

        foreach (var relative in ManifestFile.Strings(read.Node, ManifestFile.OwnedFilesKey))
        {
            var delivered = $"{layout.Root}/{relative}";
            if (!Exists(delivered))
            {
                yield return new(Severity.Critical, "owned-integrity",
                    $"{relative}: named in ownedFiles but missing from disk → re-run /legislator to restore it");
                continue;
            }

            var source = OwnedSet.SourceOf(relative, skill, layout, options);
            if (source is not null && fs.File.Exists(source)
                && !fs.File.ReadAllBytes(delivered).SequenceEqual(fs.File.ReadAllBytes(source)))
            {
                yield return new(Severity.Critical, "owned-integrity",
                    $"{relative}: diverges from the skill source → re-run /legislator to restore it byte-for-byte");
            }
        }
    }

    private IEnumerable<AuditFinding> Check04Staleness(JsonNode? manifest)
    {
        var repo = ManifestFile.Scalar(manifest, ManifestFile.VersionKey) ?? Unknown;
        if (repo != SkillVersion)
        {
            yield return new(Severity.Info, "staleness",
                $"{layout.Relative(layout.Manifest)}: legislatorVersion {repo}, skill source is v{SkillVersion} → re-run /legislator to upgrade");
        }
    }

    private IEnumerable<AuditFinding> Check05OkfIndexLinks()
    {
        var index = $"{layout.Okf}/{options.OkfIndexFile.Value}";
        if (!fs.File.Exists(index))
        {
            yield break;
        }

        foreach (var target in Links(Read(index)))
        {
            if (!Exists($"{layout.Okf}/{target}") && !Exists($"{layout.Root}/{target}"))
            {
                yield return new(Severity.Warning, "okf-index-links",
                    $"{layout.Relative(index)}: link target `{target}` does not resolve → fix or remove the link");
            }
        }
    }

    private IEnumerable<AuditFinding> Check06CodebaseMap()
    {
        var map = $"{layout.Okf}/{options.CodebaseMapFile.Value}";
        if (!fs.File.Exists(map))
        {
            yield break;
        }

        var rows = MapRow().Matches(Read(map)).Cast<Match>().Select(m => m.Groups[1].Value).ToList();
        foreach (var row in rows.Where(row => !fs.Directory.Exists($"{layout.Root}/{row.TrimEnd('/')}")))
        {
            yield return new(Severity.Warning, "codebase-map",
                $"{layout.Relative(map)}: row for `{row}` names a directory that no longer exists on disk → remove or update the row");
        }

        var mapped = rows.Select(row => row.TrimEnd('/')).ToHashSet(StringComparer.Ordinal);
        var build = options.BuildDirs.Value.ToHashSet(StringComparer.Ordinal);
        foreach (var directory in SymbolIndex.TopLevelDirectories(fs, layout.Root)
            .Where(d => !build.Contains(d) && !mapped.Contains(d)).Order(StringComparer.Ordinal))
        {
            yield return new(Severity.Warning, "codebase-map",
                $"{layout.Relative(map)}: top-level directory `{directory}/` has no row → add one");
        }
    }

    private IEnumerable<AuditFinding> Check07OrphanDocs(IReadOnlyList<string> everyDocument)
    {
        var exempt = ExemptHomes();
        foreach (var candidate in OrphanCandidates())
        {
            var relative = layout.Relative(candidate);
            if (relative == $"{layout.Relative(layout.Docs)}/{options.BacklogFile.Value}"
                || exempt.Any(prefix => relative.StartsWith(prefix, StringComparison.Ordinal)))
            {
                continue;
            }

            if (!Referenced(candidate, everyDocument))
            {
                yield return new(Severity.Warning, "orphan-docs",
                    $"{relative}: not referenced by {layout.Relative(layout.Okf)}/{options.OkfIndexFile.Value} or any other markdown file in the repo → link it in or delete it");
            }
        }
    }

    private IEnumerable<AuditFinding> Check08JournalRecency()
    {
        if (!fs.Directory.Exists(layout.Journal))
        {
            yield break;
        }

        var dates = new List<string>();
        foreach (var file in Files(layout.Journal, "*.md").Order(StringComparer.Ordinal))
        {
            if (Name(file) == options.ReadmeFile.Value)
            {
                continue;
            }

            var stem = Name(file)[..^".md".Length];
            var match = LeadingDate().Match(stem);
            var found = match.Success ? match.Value : AnyDate().Match(Read(file)) is { Success: true } m ? m.Value : null;
            if (found is not null)
            {
                dates.Add(found);
            }
        }

        dates.Sort(StringComparer.Ordinal);
        var lastCode = Git("log", "-1", "--format=%cs", "--", ".", $":(exclude){options.DocsDir.Value}");
        if (lastCode is null)
        {
            yield break;
        }

        var newest = dates.Count > 0 ? dates[^1] : null;
        var code = DateOnly.Parse(lastCode, CultureInfo.InvariantCulture);
        if (newest is null || code.DayNumber - DateOnly.Parse(newest, CultureInfo.InvariantCulture).DayNumber > options.JournalRecencyDays.Value)
        {
            yield return new(Severity.Warning, "journal-recency",
                $"{layout.Relative(layout.Journal)}/: newest entry is {newest ?? "no dated entries found"}, but the last commit touching paths outside {layout.Relative(layout.Docs)}/ is {lastCode} → write the missing entry or record why none is needed");
        }
    }

    private IEnumerable<AuditFinding> Check09ForeignStructures(JsonNode? manifest, string? entry)
    {
        var kept = ManifestFile.Objects(manifest, ManifestFile.KeepKey)
            .Select(k => k[ManifestFile.PathField]?.ToString()).ToHashSet(StringComparer.Ordinal);

        foreach (var relative in options.ForeignStructures.Value.Where(r => !kept.Contains(r)))
        {
            var path = $"{layout.Root}/{relative}";
            if (fs.Directory.Exists(path))
            {
                var inner = Files(path, "*").Order(StringComparer.Ordinal).ToList();
                foreach (var found in inner.Count > 0 ? inner.Select(layout.Relative) : [relative])
                {
                    yield return new(Severity.Info, "foreign-structures",
                        $"{found}: foreign AI-layer structure / agent-tooling debris → clean it up or fold it into the AI layer");
                }
            }
            else if (fs.File.Exists(path))
            {
                yield return new(Severity.Info, "foreign-structures",
                    $"{relative}: foreign AI-layer structure → fold its law into {options.ProjectRulesDir.Value}/ or clean it up");
            }
        }

        if (EntryDocument.IsRealAlias(fs, layout, options) && entry == options.EntryDocument.Value)
        {
            yield return new(Severity.Warning, "foreign-structures",
                $"{options.EntryAlias.Value}: is a real file beside {options.EntryDocument.Value} → it should be the symlink to {options.EntryDocument.Value} (v14 file model)");
        }
    }

    private IEnumerable<AuditFinding> Check10KeepList(JsonNode? manifest, IReadOnlyList<string> everyDocument)
    {
        if (manifest is not null && !ManifestFile.Declares(manifest, ManifestFile.KeepKey))
        {
            yield return new(Severity.Info, "keep-list",
                $"{layout.Relative(layout.Manifest)}: no keep key (pre-keep-schema manifest) → re-run /legislator to refresh");
        }

        foreach (var entry in ManifestFile.Objects(manifest, ManifestFile.KeepKey))
        {
            var kept = entry[ManifestFile.PathField]?.ToString() ?? "";
            var path = $"{layout.Root}/{kept}";
            if (!Exists(path))
            {
                yield return new(Severity.Warning, "keep-list",
                    $"{kept}: kept path missing from disk → restore it or remove the keep entry");
                continue;
            }

            if (kept.StartsWith($"{options.ProjectRulesDir.Value}/", StringComparison.Ordinal)
                || !kept.EndsWith(".md", StringComparison.Ordinal)
                || Referenced(path, everyDocument))
            {
                continue;
            }

            yield return new(Severity.Warning, "keep-list",
                $"{kept}: kept but referenced from nowhere → link it from {layout.Relative(layout.Okf)}/{options.OkfIndexFile.Value} or {options.EntryDocument.Value}");
        }
    }

    private IEnumerable<AuditFinding> Check13GlossaryVitality()
    {
        var glossary = $"{layout.Okf}/{options.GlossaryFile.Value}";
        var build = options.BuildDirs.Value.ToHashSet(StringComparer.Ordinal) ;
        var sources = SymbolIndex.TopLevelDirectories(fs, layout.Root)
            .Where(d => !build.Contains(d) && d != options.DocsDir.Value).ToList();
        if (!fs.File.Exists(glossary) || sources.Count == 0)
        {
            yield break;
        }

        var rows = Read(glossary).Split('\n')
            .Count(line => line.StartsWith('|') && !RuleRow().IsMatch(line));
        if (rows <= 1)
        {
            yield return new(Severity.Warning, "glossary-vitality",
                $"{layout.Relative(glossary)}: glossary empty in a repo with source code → seed it or add the domain's terms");
        }
    }

    private IEnumerable<AuditFinding> Check14SkillBindings()
    {
        var sanctioned = $"{layout.Root}/{options.ProjectRulesDir.Value}/{options.SkillsRuleFile.Value}";
        if (!fs.File.Exists(sanctioned))
        {
            yield break;
        }

        var homes = options.SkillHomes.Value.Select(h => $"{job.Env.HomeDirectory}/{h}").ToList();
        foreach (var name in SkillNameToken().Matches(Read(sanctioned)).Cast<Match>()
            .Select(m => m.Groups[1].Value).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            if (homes.Any(home => Exists($"{home}/{name}")))
            {
                continue;
            }

            yield return new(Severity.Info, "skill-bindings",
                $"{name}: sanctioned in {options.ProjectRulesDir.Value}/{options.SkillsRuleFile.Value} but not installed on this machine → link it (see the legislator README's \"Skill ecosystem setup\") or remove it from the list");
        }
    }

    private IEnumerable<AuditFinding> Check16LegacyHomeViolation()
    {
        var legislated = Oldest(Git("log", "--diff-filter=A", "--format=%cs", "--", layout.Relative(layout.Manifest)));
        if (legislated is null)
        {
            yield break;
        }

        foreach (var sub in options.LegacyHomeSubdirs.Value)
        {
            var home = $"{layout.Docs}/{options.LegacyHomeDir.Value}/{sub}";
            if (!fs.Directory.Exists(home))
            {
                continue;
            }

            foreach (var file in Files(home, "*").Order(StringComparer.Ordinal))
            {
                var relative = layout.Relative(file);
                var born = Oldest(Git("log", "--diff-filter=A", "--format=%cs", "--", relative));
                if (born is not null && string.CompareOrdinal(born, legislated) > 0)
                {
                    yield return new(Severity.Warning, "legacy-home-violation",
                        $"{relative}: born in a legacy home after legislation ({born}) → move it to its standard home ({layout.Relative(layout.Cases)}/BL-NNN/)");
                }
            }
        }
    }

    /// <summary>Checks 15 and 17: the anchors and okf-debt jobs, re-printed under the audit's own severity. The audit is their second caller and derives nothing of its own.</summary>
    private IEnumerable<AuditFinding> Check15OkfAnchorsAndSyncDebt()
    {
        if (!fs.Directory.Exists(layout.Okf))
        {
            yield break;
        }

        foreach (var line in AnchorsJob.Unresolved(job))
        {
            yield return new(Severity.Warning, "okf-anchors",
                $"{line.Split(" → ")[0]} → the repo no longer contains it; update the document or fix the reference");
        }

        foreach (var line in OkfDebtJob.Stale(job))
        {
            yield return new(Severity.Warning, "okf-sync-debt",
                $"{line} → update the document or state why it still holds");
        }
    }

    /// <summary>
    /// Check 18, `tracker-drift` (Warning). File-local by design: the tracker itself is never
    /// read, only what the backlog says about its own shape. Two drifts, and they are mutually
    /// exclusive - a work item ABOVE the generated mirror marker sits in the region that is
    /// meant to hold pointers, and a work item in a file with no mirror at all while the entry
    /// document records a tracker is a second source of truth for the same queue.
    /// </summary>
    private IEnumerable<AuditFinding> Check18TrackerDrift(string? entry)
    {
        var backlog = $"{layout.Docs}/{options.BacklogFile.Value}";
        if (!fs.File.Exists(backlog))
        {
            yield break;
        }

        var lines = Read(backlog).Split('\n');
        int? marker = null;
        for (var i = 0; i < lines.Length; i++)
        {
            if (MirrorMarker().IsMatch(lines[i].TrimEnd('\r')))
            {
                marker = i;
                break;
            }
        }

        var tracked = entry is not null
            && Read($"{layout.Root}/{entry}").Contains("Task tracker:", StringComparison.Ordinal);
        var where = layout.Relative(backlog);

        for (var i = 0; i < lines.Length; i++)
        {
            var item = WorkItem().Match(lines[i]);
            if (!item.Success)
            {
                continue;
            }

            if (marker is int at && i < at)
            {
                yield return new(Severity.Warning, "tracker-drift",
                    $"{where}:{i + 1}: work item {item.Groups[1].Value} above the generated mirror marker → the pointer region holds no items; move it into the tracker or below the marker");
            }
            else if (marker is null && tracked)
            {
                yield return new(Severity.Warning, "tracker-drift",
                    $"{where}:{i + 1}: work item {item.Groups[1].Value} while the entry document records a task tracker and this file carries no generated mirror → two sources of truth; migrate the item or drop the tracker line");
            }
        }
    }

    /// <summary>
    /// Check 19, `case-collisions` (Warning). An owned name that differs from a file already on
    /// disk only by case survives on Linux and collides the moment the repository is cloned onto
    /// a case-insensitive file system. The mechanism is <see cref="OwnedSet.CaseCollisions"/>,
    /// built in T-10 without a slug because the check set is law and a number was not free then.
    /// </summary>
    private IEnumerable<AuditFinding> Check19CaseCollisions(ManifestRead read)
    {
        foreach (var finding in OwnedSet.CaseCollisions(
                     fs, layout, ManifestFile.Strings(read.Node, ManifestFile.OwnedFilesKey)))
        {
            yield return new(Severity.Warning, "case-collisions", finding);
        }
    }

    /// <summary>
    /// Check 20, `arm-integrity` (Warning). Is the deterministic arm on this machine the one this
    /// edition pins, and is its binary the one the edition released? The check asks the BINARY
    /// rather than the file system, because a name on PATH proves nothing about what runs.
    ///
    /// An edition that has not been tagged has released no digests, and that is stated rather
    /// than passed over: a report that withholds without saying so reads as completeness
    /// (`core/artifact-lifecycle.md`, no silent caps).
    /// </summary>
    private IEnumerable<AuditFinding> Check20ArmIntegrity()
    {
        var release = skill.Release;
        foreach (var finding in ArmIntegrityCheck.Findings(
                     fs, job.Env, job.Proc, options, release.Edition ?? SkillVersion, release.Digests))
        {
            yield return new(Severity.Warning, "arm-integrity", finding);
        }

        if (release.Digests.Count == 0)
        {
            yield return new(Severity.Info, "arm-integrity",
                $"edition {release.Edition ?? SkillVersion} records no released digests yet → the arm's identity was checked by version alone; the digests arrive with the tag");
        }
    }

    private const string Unknown = "?";

    private string SkillVersion => skill.Version ?? Unknown;

    /// <summary>The record homes: their documents are meant to be unreferenced, so reporting them would make the worklist noise (`core/artifact-lifecycle.md`).</summary>
    private IReadOnlyList<string> ExemptHomes() =>
    [
        $"{layout.Relative(layout.Rules)}/",
        $"{layout.Relative(layout.Adr)}/",
        $"{layout.Relative(layout.Journal)}/",
        $"{layout.Relative(layout.Docs)}/{options.LegacyHomeDir.Value}/",
        $"{layout.Relative(layout.Cases)}/",
    ];

    private IEnumerable<string> OrphanCandidates()
    {
        if (fs.Directory.Exists(layout.Okf))
        {
            foreach (var file in fs.Directory.EnumerateFiles(layout.Okf, "*.md").Order(StringComparer.Ordinal))
            {
                yield return file.Replace('\\', '/');
            }
        }

        if (fs.Directory.Exists(layout.Docs))
        {
            foreach (var file in fs.Directory.EnumerateFiles(layout.Docs, "*.md").Order(StringComparer.Ordinal))
            {
                yield return file.Replace('\\', '/');
            }
        }
    }

    /// <summary>The documents check 2 reads: the entry document, the whole documentation tree and the project rules.</summary>
    private IEnumerable<string> Scanned(string? entry)
    {
        if (entry is not null)
        {
            yield return $"{layout.Root}/{entry}";
        }

        foreach (var file in Files(layout.Docs, "*.md").Order(StringComparer.Ordinal))
        {
            yield return file;
        }

        var rules = $"{layout.Root}/{options.ProjectRulesDir.Value}";
        if (fs.Directory.Exists(rules))
        {
            foreach (var file in fs.Directory.EnumerateFiles(rules, "*.md").Order(StringComparer.Ordinal))
            {
                yield return file.Replace('\\', '/');
            }
        }
    }

    /// <summary>Every markdown file that may reference another, build output and git's own tree apart.</summary>
    private List<string> AllMarkdown(string? entry)
    {
        var build = options.BuildDirs.Value.ToHashSet(StringComparer.Ordinal);
        var found = Files(layout.Root, "*.md")
            .Where(file => !layout.Relative(file).Split('/').Any(
                part => build.Contains(part) || part.StartsWith(options.GitDir.Value, StringComparison.Ordinal)))
            .ToList();
        if (entry is not null && !found.Contains($"{layout.Root}/{entry}", StringComparer.Ordinal))
        {
            found.Add($"{layout.Root}/{entry}");
        }

        return found;
    }

    /// <summary>Whether anything names this document - by its repository-relative path in text, or by a markdown link that resolves to it.</summary>
    private bool Referenced(string candidate, IReadOnlyList<string> referrers)
    {
        var relative = layout.Relative(candidate);
        foreach (var other in referrers.Where(o => o != candidate))
        {
            var text = Read(other);
            if (text.Contains(relative, StringComparison.Ordinal))
            {
                return true;
            }

            var directory = other[..other.LastIndexOf('/')];
            if (Links(text).Any(target => Resolve(directory, target) == candidate))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> Links(string text) =>
        MarkdownLink().Matches(text).Cast<Match>().Select(m => m.Groups[1].Value)
            .Where(t => !t.StartsWith("http:", StringComparison.Ordinal) && !t.StartsWith("https:", StringComparison.Ordinal));

    /// <summary>A link target against the linking document's own directory, with `.` and `..` collapsed.</summary>
    private static string Resolve(string directory, string target)
    {
        var parts = new List<string>(directory.Split('/'));
        foreach (var step in target.Split('/'))
        {
            if (step == "..")
            {
                if (parts.Count > 0)
                {
                    parts.RemoveAt(parts.Count - 1);
                }
            }
            else if (step != "." && step.Length > 0)
            {
                parts.Add(step);
            }
        }

        return string.Join('/', parts);
    }

    private string? Git(params string[] args)
    {
        var (output, available) = GitLog.Ask(job.Proc, options, layout.Root, args);
        return available
            ? output
            : throw new InvalidOperationException(
                "git unavailable — the audit's git-backed checks cannot run; install git or run where it exists");
    }

    /// <summary>The oldest of the dates git listed - a file's adding commit is the last line of an add-filtered log.</summary>
    private static string? Oldest(string? log) =>
        log?.Split('\n') is { Length: > 0 } lines ? lines[^1].Trim() : null;

    private IEnumerable<string> Files(string directory, string pattern) =>
        fs.Directory.Exists(directory)
            ? fs.Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories).Select(f => f.Replace('\\', '/'))
            : [];

    private static string Name(string path) => path[(path.LastIndexOf('/') + 1)..];

    private bool Exists(string path) => fs.File.Exists(path) || fs.Directory.Exists(path);

    private string Read(string path)
    {
        try
        {
            return fs.File.ReadAllText(path);
        }
        catch (IOException)
        {
            return "";
        }
        catch (UnauthorizedAccessException)
        {
            return "";
        }
    }
}
