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
        Baseline = Join(Ai, options.BaselineFile.Value);
        Okf = Join(Docs, options.OkfDir.Value);
        Cases = Join(Docs, options.CasesDir.Value);
        Adr = Join(Docs, options.AdrDir.Value);
        Journal = Join(Docs, options.JournalDir.Value);
        Changelog = Join(root, options.ChangelogFile.Value);
        Opencode = Join(root, options.OpencodeConfig.Value);
        RuleStacks = Join(Rules, options.StacksDir.Value);
        RulesCore = Join(Rules, options.RulesCoreDir.Value);
        LegislationImport = Relative(Join(Rules, options.LegislationMarker.Value));
    }

    public string Root { get; }

    public string Docs { get; }

    public string Ai { get; }

    public string Rules { get; }

    public string Manifest { get; }

    public string Baseline { get; }

    public string Okf { get; }

    public string Cases { get; }

    public string Adr { get; }

    public string Journal { get; }

    public string Changelog { get; }

    /// <summary>The opencode host's configuration - an owned file since v20, which is why it is part of the layout rather than a project artifact.</summary>
    public string Opencode { get; }

    /// <summary>The stack rule directories; their names are what a manifest-less repository was subscribed to.</summary>
    public string RuleStacks { get; }

    /// <summary>The core rule directory - delivered whole to every legislated repository, whatever it subscribes to.</summary>
    public string RulesCore { get; }

    /// <summary>The import line's target as the entry document writes it: repository-relative, so it can be searched for in text.</summary>
    public string LegislationImport { get; }

    /// <summary>The repository-relative, forward-slashed form of a path inside this layout - the form every finding and every manifest entry prints.</summary>
    public string Relative(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return path.Replace('\\', '/')[(Root.Length + 1)..];
    }

    private static string Join(string left, string right) => $"{left.TrimEnd('/')}/{right}";
}
