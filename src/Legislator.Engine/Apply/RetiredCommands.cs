using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;
using Legislator.Core.Options;
using Legislator.Core.Repo;

namespace Legislator.Engine.Apply;

/// <summary>
/// A file the constitution retires is a file every command still <em>running</em> it now runs in
/// vain. The retirement and the declaration have to move in <b>one act</b>: move the declaration
/// first and the entry document contradicts the rule files it imports, which still call the
/// delivered file the executing arm of their rule; move it after, and upgrade day is the day a
/// repository's declared gate names a file that is gone. Neither ordering is safe, so neither is
/// taken — the apply that retires the file rewrites the commands that run it, or refuses to
/// retire it.
/// </summary>
/// <remarks>
/// <para>
/// <b>An invocation, not a mention.</b> The subject is a line that <em>runs</em> the retired path.
/// A line that merely names it is reported and left — including the entry document's own
/// `@docs/ai/rules/core/…` import block, which names every rule file by path. The first version
/// refused on any mention, which meant it refused every upgrade that retires a rule file or drops
/// a stack: two of the five scenarios in this edition's own e2e corpus, and a stack drop on every
/// governed repository. The engine deletes and the skill's report proposes the import's removal;
/// that division is older than this sweep and is not this sweep's to override.
/// </para>
/// <para>
/// <b>Declarations are rewritten; records are counted.</b> A repository's entry document, its
/// project rules and the gate scripts it owns say how to verify it <em>now</em>. A case, a journal
/// day, an ADR, a change fragment, the changelog, a test's fixture, a generated mirror and the OKF
/// bundle's own human class (`log.md`, `glossary.md`) are records of what was true when they were
/// written — `core/okf.md` says naming something since removed is correct there, not stale — so
/// they are counted, not listed, and never rewritten. A worklist may only carry what its reader
/// can act on.
/// </para>
/// </remarks>
public static partial class RetiredCommands
{
    /// <summary>What a sweep found: the lines it would rewrite, the ones it cannot, the mentions it leaves, and the records it counted.</summary>
    public sealed record Plan(
        IReadOnlyList<Edit> Rewrites,
        IReadOnlyList<string> Refusals,
        IReadOnlyList<string> Mentions,
        int Records);

    /// <summary>One line of one declaration, before and after.</summary>
    public sealed record Edit(string Relative, int Line, string Before, string After);

    // The path may be quoted; the job is a word, so `--help` is not one and a line carrying it is
    // refused rather than turned into `legislator --help`.
    [GeneratedRegex("""(?:python3?\s+)(?<quote>["']?)(?<path>[^\s"']+)\k<quote>\s+(?<job>[A-Za-z][\w-]*)""")]
    private static partial Regex Invocation();

    /// <summary>The plan for one prospective retirement set, computed before anything is written.</summary>
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
            var relative = layout.Relative(file);
            // The record test comes first: a script under `tests/` or `fixtures/` is a frozen
            // input whatever its extension, and taking it as a declaration refused an upgrade
            // over a fixture of the old gate.
            var isRecord = IsRecord(layout, relative);
            var isDeclaration = !isRecord && declarations.Contains(file);
            var text = fs.File.ReadAllText(file);
            var lines = Split(text);
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Text;
                var named = retiring.FirstOrDefault(path => Names(line, path));
                if (named is null)
                {
                    continue;
                }

                var rewritten = isDeclaration ? Rewrite(line, named) : null;
                if (rewritten is not null)
                {
                    rewrites.Add(new Edit(relative, i + 1, line, rewritten));
                }
                else if (isDeclaration && Invokes(line, named))
                {
                    refusals.Add(
                        $"{relative}:{i + 1} runs '{named}', which this edition retires, in a form this "
                        + $"migration does not know how to rewrite: {line.Trim()}");
                }
                else if (isRecord)
                {
                    records++;
                }
                else
                {
                    mentions.Add($"{relative}:{i + 1}");
                }
            }
        }

        return new Plan(rewrites, refusals, mentions, records);
    }

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
            var lines = Split(fs.File.ReadAllText(path));
            foreach (var edit in group)
            {
                // Every line keeps the ending it had — a rewrite that normalises them turns the
                // upgrade into a whole-file diff, and one that walks only '\n' truncates a file
                // whose lines end in a lone '\r'.
                lines[edit.Line - 1] = lines[edit.Line - 1] with { Text = edit.After };
                done.Add($"{edit.Relative}:{edit.Line}");
            }

            var built = new StringBuilder();
            foreach (var line in lines)
            {
                built.Append(line.Text).Append(line.Ending);
            }

            fs.File.WriteAllText(path, built.ToString());
        }

        return done;
    }

    /// <summary>
    /// Every <c>python3 &lt;retired&gt; &lt;job&gt;</c> on the line becomes <c>legislator &lt;job&gt;</c>.
    /// Null where nothing on the line is such an invocation, or where one is and the retired path
    /// still stands after the rewrite.
    /// </summary>
    /// <remarks>
    /// <b>Every occurrence, not the first.</b> All four repositories this was written for put both
    /// invocations on one line; a rewrite that took the first and then saw the path still there
    /// refused every one of them.
    /// </remarks>
    public static string? Rewrite(string line, string retired)
    {
        ArgumentNullException.ThrowIfNull(line);

        var any = false;
        var rewritten = Invocation().Replace(line, m =>
        {
            if (!PathNames(m.Groups["path"].Value, retired))
            {
                return m.Value;
            }

            any = true;
            return $"legislator {m.Groups["job"].Value}";
        });
        return !any || Names(rewritten, retired) ? null : rewritten;
    }

    /// <summary>Whether the line runs the retired path, as opposed to merely naming it.</summary>
    public static bool Invokes(string line, string retired)
    {
        ArgumentNullException.ThrowIfNull(line);

        return Invocation().Matches(line).Any(m => PathNames(m.Groups["path"].Value, retired));
    }

    /// <summary>
    /// Whether a line names the retired path <b>as that file</b>. A plain substring test refused
    /// an upgrade over <c>tools/docs/ai/engine.py.bak</c> — a different file whose name contains
    /// this one — and a bound that rejected every <c>/</c> before it missed <c>./</c>,
    /// <c>$ROOT/</c> and an absolute path, which are the same file written three other ways.
    /// </summary>
    public static bool Names(string line, string retired)
    {
        ArgumentNullException.ThrowIfNull(line);

        var at = 0;
        while ((at = line.IndexOf(retired, at, StringComparison.Ordinal)) >= 0)
        {
            var afterAt = at + retired.Length;
            var after = afterAt >= line.Length ? ' ' : line[afterAt];
            if (!IsNameCharacter(after))
            {
                var start = at;
                while (start > 0 && (IsNameCharacter(line[start - 1]) || line[start - 1] is '/' or '$'))
                {
                    start--;
                }

                if (PathNames(line[start..afterAt], retired))
                {
                    return true;
                }
            }

            at = afterAt;
        }

        return false;
    }

    /// <summary>Whether one written path is the retired one: itself, or itself under a root.</summary>
    private static bool PathNames(string candidate, string retired)
    {
        if (string.Equals(candidate, retired, StringComparison.Ordinal))
        {
            return true;
        }

        if (!candidate.EndsWith($"/{retired}", StringComparison.Ordinal))
        {
            return false;
        }

        // `./x`, `$ROOT/x` and `/abs/x` are the same file; `other/x` is a different one.
        var prefix = candidate[..^(retired.Length + 1)];
        return prefix.Length == 0 || prefix is "." || prefix[0] is '$' or '/';
    }

    private static bool IsNameCharacter(char c) => char.IsLetterOrDigit(c) || c is '.' or '-' or '_';

    /// <summary>One line and the ending it was written with.</summary>
    private readonly record struct Line(string Text, string Ending);

    /// <summary>Splits text into lines, keeping each line's own terminator — `\r\n`, `\n`, `\r` or none.</summary>
    private static List<Line> Split(string text)
    {
        List<Line> lines = [];
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is not ('\n' or '\r'))
            {
                continue;
            }

            var ending = text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n' ? "\r\n" : text[i].ToString();
            lines.Add(new Line(text[start..i], ending));
            i += ending.Length - 1;
            start = i + 1;
        }

        if (start < text.Length)
        {
            lines.Add(new Line(text[start..], ""));
        }

        return lines;
    }

    /// <summary>
    /// The homes a declaration lives in: the entry document (and a real alias beside it), the
    /// project rules, and the <b>scripts</b> the repository owns under its tools directory —
    /// not the documents there, since a test's frozen fixture is not a declaration.
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

        foreach (var (directory, pattern) in new[]
                 {
                     ($"{layout.Root}/{options.ProjectRulesDir.Value}", "*.md"),
                     ($"{layout.Root}/tools", "*.sh"),
                 })
        {
            if (!fs.Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in fs.Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories))
            {
                yield return file.Replace('\\', '/');
            }
        }
    }

    /// <summary>Every file a mention could be in: the declarations, the changelog, and the repository's own text.</summary>
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

        foreach (var directory in new[] { $"{layout.Root}/tools", layout.Docs })
        {
            if (!fs.Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in fs.Directory.EnumerateFiles(directory, "*.md", SearchOption.AllDirectories)
                         .Select(p => p.Replace('\\', '/'))
                         .Where(p => !p.StartsWith(layout.Ai + "/", StringComparison.Ordinal))
                         .OrderBy(p => p, StringComparer.Ordinal))
            {
                yield return file;
            }
        }
    }

    /// <summary>
    /// A home whose going out of date is the design. Matched on the <b>repository-relative</b>
    /// path: keyed on the absolute one, a repository checked out under a directory called `tests`
    /// had every mention counted and none named.
    /// </summary>
    private static bool IsRecord(RepoLayout layout, string relative) =>
        relative.Equals(layout.Relative(layout.Changelog), StringComparison.Ordinal)
        || StartsWith(relative, layout.Relative(layout.Cases))
        || StartsWith(relative, layout.Relative(layout.Journal))
        || StartsWith(relative, layout.Relative(layout.Adr))
        || StartsWith(relative, layout.Relative(layout.Changes))
        // `core/okf.md`'s human class: a glossary defines terms and a log records what was true
        // at the time, so naming something since removed is correct there, not stale.
        || relative.EndsWith("/log.md", StringComparison.Ordinal)
        || relative.EndsWith("/glossary.md", StringComparison.Ordinal)
        // A generated mirror is never hand-edited, and legacy specs are history by their own law.
        || relative.EndsWith("/backlog.md", StringComparison.Ordinal)
        || StartsWith(relative, $"{layout.Relative(layout.Docs)}/superpowers")
        // A test's fixture is frozen input: its content is the thing under test.
        || relative.Contains("/tests/", StringComparison.Ordinal)
        || relative.Contains("/fixtures/", StringComparison.Ordinal);

    private static bool StartsWith(string relative, string directory) =>
        relative.StartsWith($"{directory}/", StringComparison.Ordinal);
}
