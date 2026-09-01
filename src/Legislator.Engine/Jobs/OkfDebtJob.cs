using System.Globalization;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Legislator.Core.Repo;
using Legislator.Engine.Anchors;
using Legislator.Engine.Okf;
using Legislator.Engine.Text;

namespace Legislator.Engine.Jobs;

/// <summary>
/// The other executing arm of `core/okf.md` (C-08, R-8205): it names anchored documents whose
/// sources moved on without them - an anchored source file with a commit more than the
/// declared threshold newer than the document's own newest commit. A directory anchor is never
/// a source, its history being the union of everything beneath it. Repair is an ordinary OKF
/// update by the document's owner, never an automatic rewrite.
/// </summary>
public sealed partial class OkfDebtJob : IJob
{
    public string Name => "okf-debt";

    public string Usage => Name;

    [GeneratedRegex("`([^`\n]+)`")]
    private static partial Regex Token();

    public JobResult Run(JobContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var fs = ctx.Fs;
        var layout = new RepoLayout(ctx.Options, ctx.Root);
        var topLevel = SymbolIndex.TopLevelDirectories(fs, ctx.Root).ToHashSet(StringComparer.Ordinal);
        var threshold = ctx.Options.OkfDebtDays.Value;
        var findings = new List<string>();

        foreach (var document in OkfDocuments.Anchored(fs, layout, ctx.Options.HumanClassDocs.Value))
        {
            var relative = OkfDocuments.Relative(ctx.Root, document);
            var documentIso = NewestCommit(ctx, relative);
            if (documentIso is null)
            {
                continue;                     // untracked, or no history - nothing to compare
            }

            var documentDate = DateTimeOffset.Parse(documentIso, CultureInfo.InvariantCulture);
            (string Source, int Days)? worst = null;
            foreach (var (_, line) in OkfDocuments.ScannableLines(fs.File.ReadAllText(document)))
            {
                foreach (var match in Token().Matches(line).Cast<Match>())
                {
                    var token = match.Groups[1].Value.Trim();
                    if (AnchorClassifier.Classify(token, topLevel) != AnchorKind.Path)
                    {
                        continue;
                    }

                    // A broken anchor is the anchors job's finding, and a directory's history is
                    // the union of everything beneath it, so it can never say whether one
                    // document went stale: neither is ever asked about.
                    var source = AnchorTarget.Resolve(fs, ctx.Root, token, ctx.Options.SourceExtensions.Value);
                    if (source is null || !fs.File.Exists($"{ctx.Root}/{source}"))
                    {
                        continue;
                    }

                    var sourceIso = NewestCommit(ctx, source);
                    if (sourceIso is null)
                    {
                        continue;
                    }

                    var days = (int)(DateTimeOffset.Parse(sourceIso, CultureInfo.InvariantCulture) - documentDate).TotalDays;
                    if (days > threshold && (worst is null || days > worst.Value.Days))
                    {
                        worst = (source, days);
                    }
                }
            }

            if (worst is not null)
            {
                findings.Add(
                    $"{relative}: okf-sync-debt: {worst.Value.Source} changed {worst.Value.Days} days after this document");
            }
        }

        return Findings.AsResult(findings);
    }

    /// <summary>
    /// The newest commit date of one path - and the loud stop when the instrument itself is
    /// missing. R-665 (BL-069 F1): a verification job whose measuring instrument is absent
    /// fails loud, never reports clean. The first anchored document is the probe, so a
    /// repository with nothing to measure needs no git at all.
    /// </summary>
    private static string? NewestCommit(JobContext ctx, string relative)
    {
        var (iso, available) = GitLog.NewestCommit(ctx.Proc, ctx.Options, ctx.Root, relative);
        return available
            ? iso
            : throw new InvalidOperationException(
                "git unavailable — okf-debt cannot measure staleness; install git or run where it exists");
    }
}
