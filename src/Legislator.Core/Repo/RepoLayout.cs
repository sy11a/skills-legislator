using Legislator.Core.Options;

namespace Legislator.Core.Repo;

/// <summary>
/// Where the constitution's parts live inside one repository (C-07). Every segment comes from
/// the options model, so a repository that renamed a directory is read correctly without a
/// literal moving anywhere; the segments are joined with '/' because these paths are compared
/// against document text and printed into findings, where the separator is part of the answer.
/// </summary>
public sealed class RepoLayout
{
    public RepoLayout(LegislatorOptions options, string root)
    {
        ArgumentNullException.ThrowIfNull(options);

        Root = root;
        Docs = Join(root, options.DocsDir.Value);
        Ai = Join(Docs, options.AiDir.Value);
        Rules = Join(Ai, options.RulesDir.Value);
        Manifest = Join(Ai, options.ManifestFile.Value);
        Okf = Join(Docs, options.OkfDir.Value);
        Cases = Join(Docs, options.CasesDir.Value);
    }

    public string Root { get; }

    public string Docs { get; }

    public string Ai { get; }

    public string Rules { get; }

    public string Manifest { get; }

    public string Okf { get; }

    public string Cases { get; }

    private static string Join(string left, string right) => $"{left.TrimEnd('/')}/{right}";
}
