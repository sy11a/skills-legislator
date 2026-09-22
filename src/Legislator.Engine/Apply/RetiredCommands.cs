using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Legislator.Core.Options;
using Legislator.Core.Repo;

namespace Legislator.Engine.Apply;

/// <summary>
/// A file the constitution retires is a file every command still naming it now names in vain.
/// The retirement and the declaration have to move in **one act**: move the declaration first and
/// the entry document contradicts the rule files it imports, which still call the delivered file
/// the executing arm of their rule; move it after, and upgrade day is the day a repository's
/// declared gate names a file that is gone. Neither ordering is safe, so neither is taken — the
/// apply that retires the file rewrites the declarations that name it, or refuses to retire it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Declarations are rewritten; history is not.</b> A repository's entry document, its project
/// rules and the gate scripts it owns are statements about how to verify it *now*, and an upgrade
/// that leaves them stale has published a false instruction. A case summary, a journal day or a
/// changelog entry naming the same command is a record of what was true when it was written:
/// rewriting it would falsify the record, and going out of date is the design there rather than a
/// defect (the artifact-lifecycle rule). Those are reported and left alone.
/// </para>
/// <para>
/// <b>One recognised shape, and everything else refuses.</b> The rewrite knows
/// <c>python3 &lt;retired path&gt; &lt;job&gt;</c> and turns it into <c>legislator &lt;job&gt;</c>,
/// which is the form every fleet repository's bindings already use. A declaration home naming a
/// retired path in any other shape stops the run before its first write, naming the file and the
/// line. Guessing at an unrecognised shape is how a migration silently breaks a gate it was asked
/// to protect; refusing is the whole point of putting the two acts in one.
/// </para>
/// </remarks>
public static partial class RetiredCommands
{
    /// <summary>What a sweep found: the lines it would rewrite, the ones it cannot, and the mentions it leaves.</summary>
    public sealed record Plan(
        IReadOnlyList<Edit> Rewrites,
        IReadOnlyList<string> Refusals,
        IReadOnlyList<string> Mentions,
        int Records);

    /// <summary>One line of one declaration, before and after.</summary>
    public sealed record Edit(string Relative, int Line, string Before, string After);

    [GeneratedRegex(@"(?:python3?\s+)(?<path>[^\s`'""]+)\s+(?<job>[A-Za-z][\w-]*)")]
    private static partial Regex Invocation();

    /// <summary>
    /// The plan for one prospective retirement set, computed before anything is written so that a
    /// refusal can keep its promise by having written nothing yet.
    /// </summary>
    public static Plan Of(
        IFileSystem fs,
        LegislatorOptions options,
        RepoLayout layout,
        IReadOnlyCollection<string> retiring)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(retiring);

        List<Edit> rewrites = [];
        List<string> refusals = [], mentions = [];
        var records = 0;
        if (retiring.Count == 0)
        {
            return new Plan(rewrites, refusals, mentions, 0);
        }

        var declarations = Declarations(fs, options, layout).ToHashSet(StringComparer.Ordinal);
        foreach (var file in Candidates(fs, layout, declarations))
        {
            var isDeclaration = declarations.Contains(file);
            var relative = layout.Relative(file);
            var lines = fs.File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var named = retiring.FirstOrDefault(path => Names(lines[i], path));
                if (named is null)
                {
                    continue;
                }

                if (!isDeclaration)
                {
                    // **A worklist may only list what its reader can act on.** A case summary, a
                    // journal day, an ADR and a changelog entry are records of what was true when
                    // they were written; listing seventy-six of them buries the two lines that are
                    // not — an OKF concept document naming the retired command is a document the
                    // OKF rule says must be updated when the concept changes. So the records are
                    // counted and the rest are named (`core/artifact-lifecycle.md`).
                    if (IsRecord(layout, options, file))
                    {
                        records++;
                    }
                    else
                    {
                        mentions.Add($"{relative}:{i + 1}");
                    }

                    continue;
                }

                var rewritten = Rewrite(lines[i], named);
                if (rewritten is null)
                {
                    refusals.Add(
                        $"{relative}:{i + 1} names '{named}', which this edition retires, in a form this "
                        + $"migration does not know how to rewrite: {lines[i].Trim()}");
                }
                else
                {
                    rewrites.Add(new Edit(relative, i + 1, lines[i], rewritten));
                }
            }
        }

        return new Plan(rewrites, refusals, mentions, records);
    }

    /// <summary>A home whose going out of date is the design: a case, a journal day, an ADR, the changelog.</summary>
    private static bool IsRecord(RepoLayout layout, LegislatorOptions options, string file) =>
        string.Equals(file, layout.Changelog, StringComparison.Ordinal)
        || file.StartsWith(layout.Cases + "/", StringComparison.Ordinal)
        || file.StartsWith(layout.Journal + "/", StringComparison.Ordinal)
        || file.StartsWith(layout.Adr + "/", StringComparison.Ordinal)
        || file.StartsWith(layout.Changes + "/", StringComparison.Ordinal)
        // A test's fixture is frozen input: its content is the thing under test, and rewriting it
        // would change what the test is testing. clerk pins a 2026-08-31 snapshot by commit.
        || file.Contains("/fixtures/", StringComparison.Ordinal)
        || file.Contains("/tests/", StringComparison.Ordinal);

    /// <summary>Applies a plan's edits. Never called when the plan carries a refusal.</summary>
    public static IReadOnlyList<string> Apply(IFileSystem fs, RepoLayout layout, Plan plan)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(plan);

        List<string> done = [];
        foreach (var group in plan.Rewrites.GroupBy(e => e.Relative, StringComparer.Ordinal))
        {
            var path = $"{layout.Root}/{group.Key}";
            var lines = fs.File.ReadAllLines(path);
            foreach (var edit in group)
            {
                lines[edit.Line - 1] = edit.After;
                done.Add($"{edit.Relative}:{edit.Line}");
            }

            // **Every line keeps the ending it had**, not just the last one. Reading with
            // `ReadAllLines` and joining on '\n' normalises a CRLF file wholesale, which is the
            // whole-file diff this comment used to promise it avoided.
            fs.File.WriteAllText(path, Rejoin(fs.File.ReadAllText(path), lines));
        }

        return done;
    }

    /// <summary>
    /// Every <c>python3 &lt;retired&gt; &lt;job&gt;</c> on the line becomes <c>legislator &lt;job&gt;</c>.
    /// Null where the line names the retired path in any other shape.
    /// </summary>
    /// <remarks>
    /// <b>Every occurrence, not the first.</b> The first version rewrote one match, saw the path
    /// still on the line and refused — and every one of the four repositories this was written
    /// for writes both invocations on one line, so the change refused all four and told the
    /// operator to do by hand the thing the ruling says must not be done by hand. A fixture
    /// written with one invocation per line is a fixture of a shape the fleet does not use.
    /// </remarks>
    public static string? Rewrite(string line, string retired)
    {
        ArgumentNullException.ThrowIfNull(line);

        var rewritten = Invocation().Replace(
            line,
            m => string.Equals(m.Groups["path"].Value, retired, StringComparison.Ordinal)
                ? $"legislator {m.Groups["job"].Value}"
                : m.Value);
        return Names(rewritten, retired) ? null : rewritten;
    }

    /// <summary>
    /// Whether a line names the retired path <b>as a path</b>. A plain substring test refused an
    /// upgrade over <c>tools/docs/ai/engine.py.bak</c> — a different file whose name contains
    /// this one — so the match is bounded by the characters a path is written between.
    /// </summary>
    public static bool Names(string line, string retired)
    {
        var at = 0;
        while ((at = line.IndexOf(retired, at, StringComparison.Ordinal)) >= 0)
        {
            var before = at == 0 ? ' ' : line[at - 1];
            var afterAt = at + retired.Length;
            var after = afterAt >= line.Length ? ' ' : line[afterAt];
            if (!IsPathCharacter(before) && !IsPathCharacter(after))
            {
                return true;
            }

            at = afterAt;
        }

        return false;
    }

    private static bool IsPathCharacter(char c) =>
        char.IsLetterOrDigit(c) || c is '.' or '-' or '_' or '/';

    /// <summary>Re-joins edited lines with the endings the original text used, line by line.</summary>
    private static string Rejoin(string original, string[] lines)
    {
        var built = new System.Text.StringBuilder();
        var at = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            built.Append(lines[i]);
            var end = original.IndexOf('\n', at);
            if (end < 0)
            {
                break;
            }

            built.Append(end > 0 && original[end - 1] == '\r' ? "\r\n" : "\n");
            at = end + 1;
        }

        return built.ToString();
    }

    /// <summary>
    /// The homes a declaration lives in: the entry document (and a real alias beside it), the
    /// project rules, and the scripts the repository owns under its tools directory. Owned files
    /// are not among them — they are rewritten byte-for-byte by this same run and self-heal.
    /// </summary>
    private static IEnumerable<string> Declarations(IFileSystem fs, LegislatorOptions options, RepoLayout layout)
    {
        foreach (var name in new[] { options.EntryDocument.Value, options.EntryAlias.Value })
        {
            var path = $"{layout.Root}/{name}";
            if (fs.File.Exists(path) && fs.FileInfo.New(path).LinkTarget is null)
            {
                yield return path;
            }
        }

        var rules = $"{layout.Root}/{options.ProjectRulesDir.Value}";
        if (fs.Directory.Exists(rules))
        {
            foreach (var file in fs.Directory.EnumerateFiles(rules, "*.md", SearchOption.AllDirectories))
            {
                yield return file.Replace('\\', '/');
            }
        }

        // **Under `tools/`, a script is a declaration and a document is not.** The first version
        // took `.md` here too, and clerk keeps a frozen 2026-08-31 backlog snapshot under
        // `tools/tests/fixtures/` that its own parser test pins by commit: six of its prose lines
        // name the retired path, so the upgrade refused and offered a remedy — edit it by hand —
        // that would have rewritten a record and changed a test's subject.
        var tools = $"{layout.Root}/tools";
        if (fs.Directory.Exists(tools))
        {
            foreach (var file in fs.Directory.EnumerateFiles(tools, "*.sh", SearchOption.AllDirectories))
            {
                yield return file.Replace('\\', '/');
            }
        }
    }

    /// <summary>Every file a mention could be in: the declarations, plus the repository's own text.</summary>
    private static IEnumerable<string> Candidates(IFileSystem fs, RepoLayout layout, IReadOnlyCollection<string> declarations)
    {
        foreach (var file in declarations.OrderBy(p => p, StringComparer.Ordinal))
        {
            yield return file;
        }

        if (fs.File.Exists(layout.Changelog))
        {
            yield return layout.Changelog;
        }

        // Documents under `tools/` are not declarations — the scripts there are — but a reader
        // may still want to know one names a retired command, so they are read for mentions.
        var tools = $"{layout.Root}/tools";
        if (fs.Directory.Exists(tools))
        {
            foreach (var file in fs.Directory.EnumerateFiles(tools, "*.md", SearchOption.AllDirectories)
                         .Select(p => p.Replace('\\', '/'))
                         .OrderBy(p => p, StringComparer.Ordinal))
            {
                yield return file;
            }
        }

        if (!fs.Directory.Exists(layout.Docs))
        {
            yield break;
        }

        foreach (var file in fs.Directory.EnumerateFiles(layout.Docs, "*.md", SearchOption.AllDirectories)
                     .Select(p => p.Replace('\\', '/'))
                     .Where(p => !p.StartsWith(layout.Ai + "/", StringComparison.Ordinal))
                     .OrderBy(p => p, StringComparer.Ordinal))
        {
            yield return file;
        }
    }
}
