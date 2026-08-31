using System.Globalization;

namespace Legislator.Core.Options;

/// <summary>
/// The options model - the only declared home of every path, file name, branch or tag shape,
/// threshold and version default (R-8209). Law content is never an option (R-8213). A port that
/// meets another literal adds a member here, never the literal (C-03).
/// </summary>
public sealed record LegislatorOptions
{
    /// <summary>The one character that joins and splits list options - in YAML sequences read, env values read and <see cref="Enumerate"/> rendered (C-04).</summary>
    public const char ListSeparator = ',';

    public OptionValue<string> DocsDir { get; init; } = new("docs", OptionsLayer.Defaults);

    public OptionValue<string> AiDir { get; init; } = new("ai", OptionsLayer.Defaults); // under docs

    public OptionValue<string> RulesDir { get; init; } = new("rules", OptionsLayer.Defaults); // under docs/ai

    public OptionValue<string> OkfDir { get; init; } = new("okf", OptionsLayer.Defaults);

    public OptionValue<string> CasesDir { get; init; } = new("cases", OptionsLayer.Defaults);

    public OptionValue<string> AdrDir { get; init; } = new("adr", OptionsLayer.Defaults);

    public OptionValue<string> JournalDir { get; init; } = new("journal", OptionsLayer.Defaults);

    public OptionValue<string> ManifestFile { get; init; } = new("manifest.json", OptionsLayer.Defaults);

    public OptionValue<string> BaselineFile { get; init; } = new("baseline.md", OptionsLayer.Defaults);

    public OptionValue<string> EntryDocument { get; init; } = new("AGENTS.md", OptionsLayer.Defaults);

    public OptionValue<string> EntryAlias { get; init; } = new("CLAUDE.md", OptionsLayer.Defaults);

    public OptionValue<string> OpencodeConfig { get; init; } = new("opencode.json", OptionsLayer.Defaults);

    public OptionValue<string> ProjectRulesDir { get; init; } = new(".claude/rules", OptionsLayer.Defaults);

    public OptionValue<string> BacklogFile { get; init; } = new("backlog.md", OptionsLayer.Defaults);

    public OptionValue<string> ChangelogFile { get; init; } = new("CHANGELOG.md", OptionsLayer.Defaults);

    public OptionValue<int> OkfDebtDays { get; init; } = new(30, OptionsLayer.Defaults);

    public OptionValue<string> BranchPattern { get; init; } = new("bl/{nnn}-{slug}", OptionsLayer.Defaults);

    public OptionValue<string> EditionTagPattern { get; init; } = new("v{n}", OptionsLayer.Defaults);

    public OptionValue<string> MachineConfigFile { get; init; } = new(".config/legislator/legislator.yaml", OptionsLayer.Defaults); // relative to home

    public OptionValue<string> InstanceConfigFile { get; init; } = new("legislator.yaml", OptionsLayer.Defaults);

    public OptionValue<string> RunRecordDir { get; init; } = new("legislator-runs", OptionsLayer.Defaults); // under the system temp dir

    public OptionValue<string> GitExecutable { get; init; } = new("git", OptionsLayer.Defaults);

    public OptionValue<int> GitTimeoutSeconds { get; init; } = new(10, OptionsLayer.Defaults);

    public OptionValue<IReadOnlyList<string>> SourceExtensions { get; init; } = new([".cs", ".ts", ".tsx", ".js", ".jsx", ".py", ".go", ".rs", ".java", ".kt", ".rb", ".php", ".sql", ".html", ".css"], OptionsLayer.Defaults);

    public OptionValue<IReadOnlyList<string>> BuildDirs { get; init; } = new(["bin", "obj", "node_modules", "dist"], OptionsLayer.Defaults);

    public OptionValue<IReadOnlyList<string>> HumanClassDocs { get; init; } = new(["glossary.md", "log.md"], OptionsLayer.Defaults);

    /// <summary>Every YAML/env key mapped to its member name - written by hand, asserted complete by test (C-03).</summary>
    public static IReadOnlyDictionary<string, string> KeyMap { get; } = new Dictionary<string, string>
    {
        ["docs_dir"] = nameof(DocsDir),
        ["ai_dir"] = nameof(AiDir),
        ["rules_dir"] = nameof(RulesDir),
        ["okf_dir"] = nameof(OkfDir),
        ["cases_dir"] = nameof(CasesDir),
        ["adr_dir"] = nameof(AdrDir),
        ["journal_dir"] = nameof(JournalDir),
        ["manifest_file"] = nameof(ManifestFile),
        ["baseline_file"] = nameof(BaselineFile),
        ["entry_document"] = nameof(EntryDocument),
        ["entry_alias"] = nameof(EntryAlias),
        ["opencode_config"] = nameof(OpencodeConfig),
        ["project_rules_dir"] = nameof(ProjectRulesDir),
        ["backlog_file"] = nameof(BacklogFile),
        ["changelog_file"] = nameof(ChangelogFile),
        ["okf_debt_days"] = nameof(OkfDebtDays),
        ["branch_pattern"] = nameof(BranchPattern),
        ["edition_tag_pattern"] = nameof(EditionTagPattern),
        ["machine_config_file"] = nameof(MachineConfigFile),
        ["instance_config_file"] = nameof(InstanceConfigFile),
        ["run_record_dir"] = nameof(RunRecordDir),
        ["git_executable"] = nameof(GitExecutable),
        ["git_timeout_seconds"] = nameof(GitTimeoutSeconds),
        ["source_extensions"] = nameof(SourceExtensions),
        ["build_dirs"] = nameof(BuildDirs),
        ["human_class_docs"] = nameof(HumanClassDocs),
    };

    /// <summary>The keys whose member is an <c>OptionValue&lt;int&gt;</c> - the validator parses these as integers of 1 or more; asserted against the members by test (C-04).</summary>
    public static IReadOnlySet<string> IntegerKeys { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "okf_debt_days",
        "git_timeout_seconds",
    };

    /// <summary>Every option as (key, rendered value, source): lists comma-joined, numbers invariant (C-03).</summary>
    public IEnumerable<(string Key, string Value, OptionsLayer Source)> Enumerate()
    {
        yield return ("docs_dir", DocsDir.Value, DocsDir.Source);
        yield return ("ai_dir", AiDir.Value, AiDir.Source);
        yield return ("rules_dir", RulesDir.Value, RulesDir.Source);
        yield return ("okf_dir", OkfDir.Value, OkfDir.Source);
        yield return ("cases_dir", CasesDir.Value, CasesDir.Source);
        yield return ("adr_dir", AdrDir.Value, AdrDir.Source);
        yield return ("journal_dir", JournalDir.Value, JournalDir.Source);
        yield return ("manifest_file", ManifestFile.Value, ManifestFile.Source);
        yield return ("baseline_file", BaselineFile.Value, BaselineFile.Source);
        yield return ("entry_document", EntryDocument.Value, EntryDocument.Source);
        yield return ("entry_alias", EntryAlias.Value, EntryAlias.Source);
        yield return ("opencode_config", OpencodeConfig.Value, OpencodeConfig.Source);
        yield return ("project_rules_dir", ProjectRulesDir.Value, ProjectRulesDir.Source);
        yield return ("backlog_file", BacklogFile.Value, BacklogFile.Source);
        yield return ("changelog_file", ChangelogFile.Value, ChangelogFile.Source);
        yield return ("okf_debt_days", OkfDebtDays.Value.ToString(CultureInfo.InvariantCulture), OkfDebtDays.Source);
        yield return ("branch_pattern", BranchPattern.Value, BranchPattern.Source);
        yield return ("edition_tag_pattern", EditionTagPattern.Value, EditionTagPattern.Source);
        yield return ("machine_config_file", MachineConfigFile.Value, MachineConfigFile.Source);
        yield return ("instance_config_file", InstanceConfigFile.Value, InstanceConfigFile.Source);
        yield return ("run_record_dir", RunRecordDir.Value, RunRecordDir.Source);
        yield return ("git_executable", GitExecutable.Value, GitExecutable.Source);
        yield return ("git_timeout_seconds", GitTimeoutSeconds.Value.ToString(CultureInfo.InvariantCulture), GitTimeoutSeconds.Source);
        yield return ("source_extensions", string.Join(ListSeparator, SourceExtensions.Value), SourceExtensions.Source);
        yield return ("build_dirs", string.Join(ListSeparator, BuildDirs.Value), BuildDirs.Source);
        yield return ("human_class_docs", string.Join(ListSeparator, HumanClassDocs.Value), HumanClassDocs.Source);
    }
}
