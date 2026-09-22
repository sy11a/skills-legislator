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
        IReadOnlyList<string> Mentions);

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
        if (retiring.Count == 0)
        {
            return new Plan(rewrites, refusals, mentions);
        }

        var declarations = Declarations(fs, options, layout).ToHashSet(StringComparer.Ordinal);
        foreach (var file in Candidates(fs, layout, declarations))
        {
            var isDeclaration = declarations.Contains(file);
            var relative = layout.Relative(file);
            var lines = fs.File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var named = retiring.FirstOrDefault(path => lines[i].Contains(path, StringComparison.Ordinal));
                if (named is null)
                {
                    continue;
                }

                if (!isDeclaration)
                {
                    mentions.Add($"{relative}:{i + 1}");
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

        return new Plan(rewrites, refusals, mentions);
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
            var lines = fs.File.ReadAllLines(path);
            foreach (var edit in group)
            {
                lines[edit.Line - 1] = edit.After;
                done.Add($"{edit.Relative}:{edit.Line}");
            }

            // The trailing newline the file had is the newline it keeps: a migration that
            // silently adds or drops one turns every later diff into a whole-file change.
            var text = string.Join('\n', lines);
            var had = fs.File.ReadAllText(path);
            fs.File.WriteAllText(path, had.EndsWith('\n') ? text + "\n" : text);
        }

        return done;
    }

    /// <summary>
    /// <c>python3 docs/ai/engine.py anchors</c> becomes <c>legislator anchors</c>, wherever in the
    /// line it stands. Null where the retired path is named in any other shape — a bare path, a
    /// prose sentence, an import — because a rewrite that guesses is worse than a refusal.
    /// </summary>
    public static string? Rewrite(string line, string retired)
    {
        var match = Invocation().Match(line);
        if (!match.Success || !string.Equals(match.Groups["path"].Value, retired, StringComparison.Ordinal))
        {
            return null;
        }

        var replaced = line[..match.Index] + $"legislator {match.Groups["job"].Value}"
            + line[(match.Index + match.Length)..];
        return replaced.Contains(retired, StringComparison.Ordinal) ? null : replaced;
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

        foreach (var directory in new[] { $"{layout.Root}/{options.ProjectRulesDir.Value}", $"{layout.Root}/tools" })
        {
            if (!fs.Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in fs.Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".md", StringComparison.Ordinal) || file.EndsWith(".sh", StringComparison.Ordinal))
                {
                    yield return file.Replace('\\', '/');
                }
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
