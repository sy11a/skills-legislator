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
/// <b>Law is imported; a tool is run.</b> The sweep asks first <em>what</em> is being retired.
/// A rule file under the rules directory is named by every entry document that imports it, and
/// retiring one is the ordinary business of a constitution — the engine deletes it and the
/// skill's report proposes the import's removal, a division older than this sweep. So a retired
/// rule is a mention everywhere and a refusal nowhere. Refusing on it refused two of the five
/// scenarios in this edition's own e2e corpus and every stack drop on the fleet.
/// </para>
/// <para>
/// <b>For a retired tool, refusal is the default and rewriting the exception.</b> A line in a
/// declaration home that names a retired executable and does not come out of the rewrite clean
/// stops the run. The version before this one refused only where its own regex had already
/// matched — so a line the rewrite <em>could</em> read was guarded and a line it could not was
/// waved through as a mention, the file deleted under a live command at exit 0. That is the
/// wrong way round: an unreadable invocation is exactly the case a migration must not guess at,
/// and an unknown classification fails toward the cheap error (`core/artifact-lifecycle.md`) —
/// a refusal costs one hand-corrected line and a re-run, a silent pass costs a broken gate.
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

        // **A tool is something a repository could be asked to run.** "Everything that is not
        // law" made a retired README, a hand-listed manifest and a rules directory under another
        // name into tools, so a bare mention of one refused the upgrade — and a `rules_dir` the
        // options model accepts with a trailing slash produced a prefix no path starts with,
        // which turned every future rule retirement on that repository into a refusal.
        var law = layout.Relative(layout.Rules).TrimEnd('/');
        var tools = retiring.Where(p => IsRunnable(p) && !StartsWith(p, law)).ToList();
        var declarations = Declarations(fs, options, layout).ToHashSet(StringComparer.Ordinal);
        foreach (var file in Candidates(fs, layout, declarations))
        {
            var relative = layout.Relative(file);
            var isDeclaration = declarations.Contains(file);
            var isRecord = IsRecord(options, layout, relative);
            var lines = Split(fs.File.ReadAllText(file));
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Text;
                // **Every path the line names, not the first.** Picking one made the law/tool
                // split depend on manifest sort order: a line naming both a retired rule and a
                // retired tool was classified by whichever sorted earlier, and a tool sorting
                // after the rules directory was deleted under a live command at exit 0.
                // **Every retired tool the line names.** Passing one to the rewrite let a second
                // ride through on the first's clean bill: `legislator anchors && python3
                // docs/ai/zz-tool.py check` came out rewritten, at exit 0, with `zz-tool.py`
                // deleted under the half that still runs it.
                var namedTools = tools.Where(path => Names(line, path)).ToList();
                var named = namedTools.FirstOrDefault()
                    ?? retiring.FirstOrDefault(path => Names(line, path));
                if (named is null)
                {
                    continue;
                }

                // A comment is neither rewritten nor refused: rewriting one falsifies a record
                // of what the gate used to be, and refusing offers a remedy that would.
                var rewritten = isDeclaration && !IsComment(relative, line)
                    ? Rewrite(line, namedTools)
                    : null;
                if (rewritten is not null)
                {
                    rewrites.Add(new Edit(relative, i + 1, line, rewritten));
                }
                else if (isDeclaration && namedTools.Count > 0 && !IsComment(relative, line))
                {
                    // Refusal is the default here, whether or not the rewrite could read the
                    // line: the shapes it cannot read are the ones a migration must not guess at.
                    refusals.Add(
                        $"{relative}:{i + 1} names '{named}', which this edition retires, and this "
                        + $"migration cannot rewrite the line: {line.Trim()}");
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
    public static string? Rewrite(string line, string retired) => Rewrite(line, [retired]);

    /// <summary>The same, over every retired tool the line names: all of them must come out clean.</summary>
    public static string? Rewrite(string line, IReadOnlyCollection<string> retired)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(retired);

        if (retired.Count == 0)
        {
            return null;
        }

        var any = false;
        var rewritten = Invocation().Replace(line, m =>
        {
            if (!retired.Any(one => PathNames(m.Groups["path"].Value, one)))
            {
                return m.Value;
            }

            // The job has to be one the binary answers to. `--help` is not a job, and neither
            // is the next word of a sentence: rewriting either produces a command that does not
            // exist, which is a worse declaration than the one it replaced.
            var job = m.Groups["job"].Value;
            if (!JobRegistry.Jobs.ContainsKey(job))
            {
                return m.Value;
            }

            any = true;
            return $"legislator {job}";
        });
        return !any || retired.Any(one => Names(rewritten, one)) ? null : rewritten;
    }

    /// <summary>
    /// Whether a retired path is something a repository could run. A document and a machine-read
    /// data file are read, never invoked: naming one is a mention, and refusing an upgrade over
    /// the sentence "never hand-edit `docs/ai/manifest.json`" helps nobody.
    /// </summary>
    private static bool IsRunnable(string path)
    {
        // **An allowlist, not a denylist.** Five excluded extensions made every other retired
        // file a tool — a `.png`, a `.csv`, a `.MD` in another case — and a bare mention of one
        // refused the upgrade. A tool is a shape something is written in to be run, or a file
        // with no extension at all.
        var name = path[(path.LastIndexOf('/') + 1)..];
        var dot = name.LastIndexOf('.');
        if (dot < 0)
        {
            return true;
        }

        return RunnableExtensions.Contains(name[dot..], StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A comment <b>in a script</b>. A fixture script that <em>describes</em> the old gate is not
    /// a gate, and stopping an upgrade over one offers a remedy — edit it by hand — that would
    /// rewrite a record.
    /// </summary>
    /// <remarks>
    /// <b>`#` is shell grammar, not Markdown's.</b> The first version applied it to every
    /// declaration home, so a heading or an issue line in an entry document — `# Gates` above the
    /// command, `#142 removed the old gate` — exempted itself and the tool was deleted under it
    /// at exit 0. A script is where a `#` line is a comment; in Markdown it is a heading, and a
    /// heading naming a retired tool is as much a declaration as the line below it. A shebang is
    /// not a comment in either.
    /// </remarks>
    public static bool IsComment(string relative, string line)
    {
        ArgumentNullException.ThrowIfNull(relative);
        ArgumentNullException.ThrowIfNull(line);

        // A `#` line is a comment in every shape a gate is written in except Markdown, where it
        // opens a heading. Keying the exemption on `.sh` while the homes grew to seven shapes
        // left a comment in a `.py`, a workflow or a `Makefile` rewritten.
        if (relative.EndsWith(".md", StringComparison.Ordinal))
        {
            return false;
        }

        var trimmed = line.TrimStart();
        return trimmed.StartsWith('#') && !trimmed.StartsWith("#!", StringComparison.Ordinal);
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

    /// <summary>Whether a path is a symbolic link — a thing that is not the file it names.</summary>
    private static bool IsLink(IFileSystem fs, string path)
    {
        try
        {
            return fs.FileInfo.New(path).LinkTarget is not null
                || fs.DirectoryInfo.New(path).LinkTarget is not null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    /// <summary>
    /// Every file under one directory, one level at a time, entering no link and leaving no
    /// directory it cannot read. A recursive enumeration does both, and each cost an upgrade:
    /// one wrote into another repository, one failed the run on `/usr/share`.
    /// </summary>
    private static List<string> Walk(IFileSystem fs, RepoLayout layout, string directory)
    {
        List<string> found = [], pending = [directory];
        while (pending.Count > 0)
        {
            var at = pending[^1];
            pending.RemoveAt(pending.Count - 1);
            string[] files, directories;
            try
            {
                files = fs.Directory.GetFiles(at);
                directories = fs.Directory.GetDirectories(at);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A directory this run cannot read is named by nothing it can check; refusing the
                // whole upgrade over one is a worse answer than reading what is readable.
                continue;
            }

            foreach (var file in files)
            {
                var normalised = file.Replace('\\', '/');
                if (!IsLink(fs, file) && normalised.StartsWith(layout.Root + "/", StringComparison.Ordinal))
                {
                    found.Add(normalised);
                }
            }

            pending.AddRange(directories.Where(d => !IsLink(fs, d)));
        }

        found.Sort(StringComparer.Ordinal);
        return found;
    }

    /// <summary>Whether a file is something a gate is written in: a known script shape, a workflow, or an executable.</summary>
    private static bool IsScript(IFileSystem fs, string file)
    {
        var name = file.Replace('\\', '/');
        var leaf = name[(name.LastIndexOf('/') + 1)..];
        var dot = leaf.LastIndexOf('.');
        if (dot >= 0)
        {
            return ScriptExtensions.Contains(leaf[dot..]);
        }

        // No extension: on a POSIX host, read it only where the repository marked it runnable.
        // Windows has no such bit, and guessing there would read every extensionless file.
        if (!OperatingSystem.IsWindows())
        {
            try
            {
                return (fs.File.GetUnixFileMode(file) & UnixFileMode.UserExecute) != 0;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>The shapes something written to be run is written in.</summary>
    private static readonly HashSet<string> RunnableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".sh", ".bash", ".zsh", ".py", ".rb", ".pl", ".ps1", ".js", ".mjs", ".cjs", ".exe",
    };

    /// <summary>The shapes a gate is written in, beside an executable with no extension at all.</summary>
    private static readonly HashSet<string> ScriptExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".sh", ".bash", ".zsh", ".py", ".ps1", ".yml", ".yaml",
    };

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

        var rules = $"{layout.Root}/{options.ProjectRulesDir.Value}";
        if (fs.Directory.Exists(rules) && !IsLink(fs, rules))
        {
            foreach (var file in Walk(fs, layout, rules))
            {
                if (file.EndsWith(".md", StringComparison.Ordinal))
                {
                    yield return file;
                }
            }
        }

        // **Where a repository may plausibly run its gate from.** `tools/*.sh` alone covered
        // foundry's `gate.sh` and nothing else: a `Makefile`, a workflow, a `scripts/` directory
        // or a `gate.bash` each ran the retired file and was neither rewritten, refused nor
        // mentioned. A file with no extension is read only where it is marked executable.
        //
        // **Never through a link, and never outside the root.** A recursive enumeration follows
        // a directory symlink and yields a file symlink as a file, so a `scripts` link to another
        // repository had that repository's gate rewritten at exit 0 — the write leaving no trace
        // in the tree the run was pointed at — and a link into `/usr/share` or a dangling one
        // failed the whole upgrade with an unhandled exception. A migration writes inside the
        // repository it was given, or it does not write.
        foreach (var directory in new[] { $"{layout.Root}/tools", $"{layout.Root}/scripts", $"{layout.Root}/.github" })
        {
            if (!fs.Directory.Exists(directory) || IsLink(fs, directory))
            {
                continue;
            }

            foreach (var file in Walk(fs, layout, directory))
            {
                if (IsScript(fs, file))
                {
                    yield return file;
                }
            }
        }

        foreach (var name in new[] { "Makefile", "makefile", "Justfile", "justfile" })
        {
            var path = $"{layout.Root}/{name}";
            if (fs.File.Exists(path) && !IsLink(fs, path))
            {
                yield return path;
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
            if (!fs.Directory.Exists(directory) || IsLink(fs, directory))
            {
                continue;
            }

            // The same walk as the declarations use: a mention reported from outside the
            // repository is a line no reader of this repository can act on, and a link into a
            // large or unreadable tree failed the run rather than being skipped.
            foreach (var file in Walk(fs, layout, directory)
                         .Where(p => p.EndsWith(".md", StringComparison.Ordinal))
                         .Where(p => !p.StartsWith(layout.Ai + "/", StringComparison.Ordinal)))
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
    private static bool IsRecord(LegislatorOptions options, RepoLayout layout, string relative)
    {
        var docs = layout.Relative(layout.Docs);
        var okf = layout.Relative(layout.Okf);
        return relative.Equals(layout.Relative(layout.Changelog), StringComparison.Ordinal)
            || StartsWith(relative, layout.Relative(layout.Cases))
            || StartsWith(relative, layout.Relative(layout.Journal))
            || StartsWith(relative, layout.Relative(layout.Adr))
            || StartsWith(relative, layout.Relative(layout.Changes))
            // `core/okf.md`'s human class: a glossary defines terms and a log records what was
            // true at the time, so naming something since removed is correct there, not stale.
            || options.HumanClassDocs.Value.Any(
                name => relative.Equals($"{okf}/{name}", StringComparison.Ordinal))
            // A generated mirror is never hand-edited; legacy specs are history by their own law.
            || relative.Equals($"{docs}/{options.BacklogFile.Value}", StringComparison.Ordinal)
            || StartsWith(relative, $"{docs}/superpowers")
            // A test's fixture is frozen input — but only a document. A *script* under the same
            // path is a live command wherever it lives, and calling it history left two of them
            // running a file the same run had deleted.
            || (relative.EndsWith(".md", StringComparison.Ordinal)
                && (relative.Contains("/tests/", StringComparison.Ordinal)
                    || relative.Contains("/fixtures/", StringComparison.Ordinal)));
    }

    private static bool StartsWith(string relative, string directory) =>
        relative.StartsWith($"{directory}/", StringComparison.Ordinal);
}
