using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Legislator.Core.Repo;
using Legislator.Engine.Anchors;
using Legislator.Engine.Okf;
using Legislator.Engine.Text;

namespace Legislator.Engine.Jobs;

/// <summary>
/// The executing arm of the OKF link-hardness rule (C-07, R-8205): every path and symbol the
/// knowledge layer backticks still exists in this repository. It writes nothing and reports
/// every anchor that no longer resolves - a finding means a document describes code that is
/// gone.
/// </summary>
public sealed partial class AnchorsJob : IJob
{
    public string Name => "anchors";

    public string Usage => Name;

    [GeneratedRegex("`([^`\n]+)`")]
    private static partial Regex Token();

    public JobResult Run(JobContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var layout = new RepoLayout(ctx.Options, ctx.Root);
        var fs = ctx.Fs;
        var topLevel = SymbolIndex.TopLevelDirectories(fs, ctx.Root).ToHashSet(StringComparer.Ordinal);
        var index = SymbolIndex.Build(fs, ctx.Root, ctx.Options);
        var roots = index.SourceRoots.Count > 0
            ? string.Join(", ", index.SourceRoots.Select(name => name + "/"))
            : "(no source roots)";

        var findings = new List<string>();
        var sites = new List<(string Document, int Line, string Token)>();

        foreach (var document in OkfDocuments.Anchored(fs, layout, ctx.Options.HumanClassDocs.Value))
        {
            var relative = OkfDocuments.Relative(ctx.Root, document);
            foreach (var (lineNumber, line) in OkfDocuments.ScannableLines(fs.File.ReadAllText(document)))
            {
                foreach (var match in Token().Matches(line).Cast<Match>())
                {
                    var token = match.Groups[1].Value.Trim();
                    switch (AnchorClassifier.Classify(token, topLevel))
                    {
                        case AnchorKind.Path when !PathResolves(fs, ctx, token):
                            findings.Add($"{relative}:{lineNumber}: path-anchor: {token} → no such file");
                            break;
                        case AnchorKind.Symbol:
                            sites.Add((relative, lineNumber, token));
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        foreach (var (document, line, token) in sites)
        {
            var leading = token.Split('.', 2)[0];
            if (!index.Contains(leading))
            {
                findings.Add($"{document}:{line}: symbol-anchor: {token} → not found in {roots}");
            }
        }

        return Findings.AsResult(findings);
    }

    private static bool PathResolves(IFileSystem fs, JobContext ctx, string token) =>
        AnchorTarget.Resolve(fs, ctx.Root, token, ctx.Options.SourceExtensions.Value) is not null;
}
