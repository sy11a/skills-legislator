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

    /// <summary>A file past this many bytes is not prose or source, so the symbol scan skips it.</summary>
    public OptionValue<int> MaxFileBytes { get; init; } = new(2_000_000, OptionsLayer.Defaults);

    public OptionValue<string> EngineFile { get; init; } = new("engine.py", OptionsLayer.Defaults); // under docs/ai

    public OptionValue<string> StacksDir { get; init; } = new("stacks", OptionsLayer.Defaults); // under docs/ai/rules

    /// <summary>The import whose presence in the entry document says the layer is already installed, so a manifest-less repository is an upgrade to reconstruct rather than a migration.</summary>
    public OptionValue<string> LegislationMarker { get; init; } = new("core/okf.md", OptionsLayer.Defaults); // under docs/ai/rules

    public OptionValue<string> SkillVersionFile { get; init; } = new("VERSION", OptionsLayer.Defaults); // under the skill package

    /// <summary>The stack a project file of one of <see cref="DotnetProjectPatterns"/> makes a candidate for; also the name of its directory under the skill's rule stacks.</summary>
    public OptionValue<string> DotnetStack { get; init; } = new("dotnet", OptionsLayer.Defaults);

    /// <summary>The stack <see cref="AureliaMarkerFile"/> or a package of the same name makes a candidate for.</summary>
    public OptionValue<string> AureliaStack { get; init; } = new("aurelia", OptionsLayer.Defaults);

    public OptionValue<IReadOnlyList<string>> DotnetProjectPatterns { get; init; } = new(["*.slnx", "*.sln", "*.csproj"], OptionsLayer.Defaults);

    public OptionValue<string> NodePackageFile { get; init; } = new("package.json", OptionsLayer.Defaults);

    public OptionValue<string> AureliaMarkerFile { get; init; } = new("aurelia_project/aurelia.json", OptionsLayer.Defaults);

    public OptionValue<string> OkfIndexFile { get; init; } = new("index.md", OptionsLayer.Defaults); // under docs/okf

    public OptionValue<string> CodebaseMapFile { get; init; } = new("codebase-map.md", OptionsLayer.Defaults); // under docs/okf

    public OptionValue<string> GlossaryFile { get; init; } = new("glossary.md", OptionsLayer.Defaults); // under docs/okf

    public OptionValue<string> AdrTemplateFile { get; init; } = new("template.md", OptionsLayer.Defaults); // under docs/adr

    public OptionValue<string> ReadmeFile { get; init; } = new("README.md", OptionsLayer.Defaults);

    public OptionValue<string> GitDir { get; init; } = new(".git", OptionsLayer.Defaults);

    /// <summary>The legacy home of pre-case specs and plans; a document born there after legislation is a finding, and everything already in it is history.</summary>
    public OptionValue<string> LegacyHomeDir { get; init; } = new("superpowers", OptionsLayer.Defaults); // under docs

    public OptionValue<IReadOnlyList<string>> LegacyHomeSubdirs { get; init; } = new(["specs", "plans"], OptionsLayer.Defaults);

    /// <summary>How far the journal may lag the last commit outside the documentation tree before the audit says so. Distinct from <see cref="OkfDebtDays"/>: two cadences that happen to agree today.</summary>
    public OptionValue<int> JournalRecencyDays { get; init; } = new(30, OptionsLayer.Defaults);

    /// <summary>Foreign AI-layer structures and agent-tooling debris - each either folded into the layer or cleaned up.</summary>
    public OptionValue<IReadOnlyList<string>> ForeignStructures { get; init; } = new([".cursorrules", ".cursor", ".github/copilot-instructions.md", "wiki", ".superpowers", ".specify", "adrs", "doc/adr", ".claude/plans", "CONTEXT.md", "CONTEXT-MAP.md", "UBIQUITOUS_LANGUAGE.md", "NOTES.md", "docs/agents", ".scratch"], OptionsLayer.Defaults);

    /// <summary>Where a sanctioned skill may be installed on this machine, relative to the home directory.</summary>
    public OptionValue<IReadOnlyList<string>> SkillHomes { get; init; } = new([".claude/skills", ".agents/skills", ".config/opencode/skills"], OptionsLayer.Defaults);

    public OptionValue<string> SkillsRuleFile { get; init; } = new("skills.md", OptionsLayer.Defaults); // under the project rules dir

    public OptionValue<string> SkillRulesPath { get; init; } = new("assets/rules", OptionsLayer.Defaults); // under the skill package

    public OptionValue<string> SkillEnginePath { get; init; } = new("assets/engine/engine.py", OptionsLayer.Defaults); // under the skill package

    public OptionValue<string> SkillOpencodeTemplate { get; init; } = new("assets/templates/opencode.json.tpl", OptionsLayer.Defaults); // under the skill package

    /// <summary>The package's own procedure document - Step 4's table is where the scaffold targets are declared, and apply snapshots them before and after a run.</summary>
    public OptionValue<string> SkillFile { get; init; } = new("SKILL.md", OptionsLayer.Defaults); // under the skill package

    /// <summary>The rule directory every legislated repository gets whatever it is subscribed to; the stacks beside it are chosen, this one is not.</summary>
    public OptionValue<string> RulesCoreDir { get; init; } = new("core", OptionsLayer.Defaults); // under docs/ai/rules and under the package rules path

    /// <summary>The names a repository's default branch conventionally carries; the conduct guard falls back to them only when exactly one is present, an ambiguity being a case it cannot decide.</summary>
    public OptionValue<IReadOnlyList<string>> ConventionalDefaultBranches { get; init; } = new(["main", "master"], OptionsLayer.Defaults);

    /// <summary>The sources directory the OKF-sync reminder watches - the half of okf.md's law that says code moved.</summary>
    public OptionValue<string> SrcDir { get; init; } = new("src", OptionsLayer.Defaults);

    /// <summary>The variable naming the executable search path - read through the injected environment, never through the process's own (R-8204).</summary>
    public OptionValue<string> PathVariable { get; init; } = new("PATH", OptionsLayer.Defaults);

    /// <summary>The extensions an executable may carry where a bare name does not resolve; empty on a POSIX machine, which is why the bare name is tried first.</summary>
    public OptionValue<IReadOnlyList<string>> ExecutableExtensions { get; init; } = new([".exe", ".cmd", ".bat"], OptionsLayer.Defaults);

    public OptionValue<string> DotnetExecutable { get; init; } = new("dotnet", OptionsLayer.Defaults);

    public OptionValue<string> NpxExecutable { get; init; } = new("npx", OptionsLayer.Defaults);

    /// <summary>The formatter this repository's own stack owns; the extension decides which of the two the format hook reaches for.</summary>
    public OptionValue<string> PrettierExecutable { get; init; } = new("prettier", OptionsLayer.Defaults);

    /// <summary>A ceiling per formatter invocation, under the whole hook's own timeout in hooks.json - best-effort polish never holds a session open.</summary>
    public OptionValue<int> FormatterTimeoutSeconds { get; init; } = new(8, OptionsLayer.Defaults);

    public OptionValue<string> CSharpExtension { get; init; } = new(".cs", OptionsLayer.Defaults);

    public OptionValue<IReadOnlyList<string>> PrettierExtensions { get; init; } = new([".ts", ".tsx", ".js", ".jsx", ".html", ".css"], OptionsLayer.Defaults);

    public OptionValue<IReadOnlyList<string>> PrettierConfigFiles { get; init; } = new([".prettierrc", ".prettierrc.json", ".prettierrc.yml", ".prettierrc.yaml", ".prettierrc.js", ".prettierrc.cjs", "prettier.config.js", "prettier.config.cjs"], OptionsLayer.Defaults);

    /// <summary>The key that turns the node package file into a prettier configuration of its own.</summary>
    public OptionValue<string> PrettierPackageKey { get; init; } = new("prettier", OptionsLayer.Defaults);

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
        ["max_file_bytes"] = nameof(MaxFileBytes),
        ["engine_file"] = nameof(EngineFile),
        ["stacks_dir"] = nameof(StacksDir),
        ["legislation_marker"] = nameof(LegislationMarker),
        ["skill_version_file"] = nameof(SkillVersionFile),
        ["dotnet_stack"] = nameof(DotnetStack),
        ["aurelia_stack"] = nameof(AureliaStack),
        ["dotnet_project_patterns"] = nameof(DotnetProjectPatterns),
        ["node_package_file"] = nameof(NodePackageFile),
        ["aurelia_marker_file"] = nameof(AureliaMarkerFile),
        ["okf_index_file"] = nameof(OkfIndexFile),
        ["codebase_map_file"] = nameof(CodebaseMapFile),
        ["glossary_file"] = nameof(GlossaryFile),
        ["adr_template_file"] = nameof(AdrTemplateFile),
        ["readme_file"] = nameof(ReadmeFile),
        ["git_dir"] = nameof(GitDir),
        ["conventional_default_branches"] = nameof(ConventionalDefaultBranches),
        ["src_dir"] = nameof(SrcDir),
        ["path_variable"] = nameof(PathVariable),
        ["executable_extensions"] = nameof(ExecutableExtensions),
        ["dotnet_executable"] = nameof(DotnetExecutable),
        ["npx_executable"] = nameof(NpxExecutable),
        ["prettier_executable"] = nameof(PrettierExecutable),
        ["formatter_timeout_seconds"] = nameof(FormatterTimeoutSeconds),
        ["csharp_extension"] = nameof(CSharpExtension),
        ["prettier_extensions"] = nameof(PrettierExtensions),
        ["prettier_config_files"] = nameof(PrettierConfigFiles),
        ["prettier_package_key"] = nameof(PrettierPackageKey),
        ["legacy_home_dir"] = nameof(LegacyHomeDir),
        ["legacy_home_subdirs"] = nameof(LegacyHomeSubdirs),
        ["journal_recency_days"] = nameof(JournalRecencyDays),
        ["foreign_structures"] = nameof(ForeignStructures),
        ["skill_homes"] = nameof(SkillHomes),
        ["skills_rule_file"] = nameof(SkillsRuleFile),
        ["skill_rules_path"] = nameof(SkillRulesPath),
        ["skill_engine_path"] = nameof(SkillEnginePath),
        ["skill_opencode_template"] = nameof(SkillOpencodeTemplate),
        ["skill_file"] = nameof(SkillFile),
        ["rules_core_dir"] = nameof(RulesCoreDir),
    };

    /// <summary>The keys whose member is an <c>OptionValue&lt;int&gt;</c> - the validator parses these as integers of 1 or more; asserted against the members by test (C-04).</summary>
    public static IReadOnlySet<string> IntegerKeys { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "okf_debt_days",
        "git_timeout_seconds",
        "max_file_bytes",
        "journal_recency_days",
        "formatter_timeout_seconds",
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
        yield return ("max_file_bytes", MaxFileBytes.Value.ToString(CultureInfo.InvariantCulture), MaxFileBytes.Source);
        yield return ("engine_file", EngineFile.Value, EngineFile.Source);
        yield return ("stacks_dir", StacksDir.Value, StacksDir.Source);
        yield return ("legislation_marker", LegislationMarker.Value, LegislationMarker.Source);
        yield return ("skill_version_file", SkillVersionFile.Value, SkillVersionFile.Source);
        yield return ("dotnet_stack", DotnetStack.Value, DotnetStack.Source);
        yield return ("aurelia_stack", AureliaStack.Value, AureliaStack.Source);
        yield return ("dotnet_project_patterns", string.Join(ListSeparator, DotnetProjectPatterns.Value), DotnetProjectPatterns.Source);
        yield return ("node_package_file", NodePackageFile.Value, NodePackageFile.Source);
        yield return ("aurelia_marker_file", AureliaMarkerFile.Value, AureliaMarkerFile.Source);
        yield return ("okf_index_file", OkfIndexFile.Value, OkfIndexFile.Source);
        yield return ("codebase_map_file", CodebaseMapFile.Value, CodebaseMapFile.Source);
        yield return ("glossary_file", GlossaryFile.Value, GlossaryFile.Source);
        yield return ("adr_template_file", AdrTemplateFile.Value, AdrTemplateFile.Source);
        yield return ("readme_file", ReadmeFile.Value, ReadmeFile.Source);
        yield return ("git_dir", GitDir.Value, GitDir.Source);
        yield return ("legacy_home_dir", LegacyHomeDir.Value, LegacyHomeDir.Source);
        yield return ("legacy_home_subdirs", string.Join(ListSeparator, LegacyHomeSubdirs.Value), LegacyHomeSubdirs.Source);
        yield return ("journal_recency_days", JournalRecencyDays.Value.ToString(CultureInfo.InvariantCulture), JournalRecencyDays.Source);
        yield return ("foreign_structures", string.Join(ListSeparator, ForeignStructures.Value), ForeignStructures.Source);
        yield return ("skill_homes", string.Join(ListSeparator, SkillHomes.Value), SkillHomes.Source);
        yield return ("skills_rule_file", SkillsRuleFile.Value, SkillsRuleFile.Source);
        yield return ("skill_rules_path", SkillRulesPath.Value, SkillRulesPath.Source);
        yield return ("skill_engine_path", SkillEnginePath.Value, SkillEnginePath.Source);
        yield return ("skill_opencode_template", SkillOpencodeTemplate.Value, SkillOpencodeTemplate.Source);
        yield return ("skill_file", SkillFile.Value, SkillFile.Source);
        yield return ("rules_core_dir", RulesCoreDir.Value, RulesCoreDir.Source);
        yield return ("conventional_default_branches", string.Join(ListSeparator, ConventionalDefaultBranches.Value), ConventionalDefaultBranches.Source);
        yield return ("src_dir", SrcDir.Value, SrcDir.Source);
        yield return ("path_variable", PathVariable.Value, PathVariable.Source);
        yield return ("executable_extensions", string.Join(ListSeparator, ExecutableExtensions.Value), ExecutableExtensions.Source);
        yield return ("dotnet_executable", DotnetExecutable.Value, DotnetExecutable.Source);
        yield return ("npx_executable", NpxExecutable.Value, NpxExecutable.Source);
        yield return ("prettier_executable", PrettierExecutable.Value, PrettierExecutable.Source);
        yield return ("formatter_timeout_seconds", FormatterTimeoutSeconds.Value.ToString(CultureInfo.InvariantCulture), FormatterTimeoutSeconds.Source);
        yield return ("csharp_extension", CSharpExtension.Value, CSharpExtension.Source);
        yield return ("prettier_extensions", string.Join(ListSeparator, PrettierExtensions.Value), PrettierExtensions.Source);
        yield return ("prettier_config_files", string.Join(ListSeparator, PrettierConfigFiles.Value), PrettierConfigFiles.Source);
        yield return ("prettier_package_key", PrettierPackageKey.Value, PrettierPackageKey.Source);
    }
}
