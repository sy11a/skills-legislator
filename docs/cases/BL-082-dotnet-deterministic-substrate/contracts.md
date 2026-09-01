# BL-082 — Contracts

Interfaces, schemas and command-line shapes the plan's tasks produce and consume. Extracted from `plan.md`'s per-task *Interfaces* blocks on 2026-08-30 (dev-flow stage 3 audit, operator ruling: contracts get an address of their own). Each contract carries the R-lines it serves and its consumers; a task cites `C-NN`, never restates it. Changing a contract changes this file first, then every consumer listed.

**Cross-cutting — AOT-first (research.md §9):** every shape that is serialized (`config show --json`, `version --json`, `detect` JSON, the run record, `HookPayload`) has a source-generated `JsonSerializerContext`; YAML is read through YamlDotNet's event parser, never a reflection `Deserializer`; every regex in Core/Engine is `[GeneratedRegex]`; Core/Engine/Hooks build with `IsAotCompatible=true`, the CLI publishes `PublishAot=true`; tests (JIT) exercise the published binary wherever a process is spawned.

## C-01 Solution layout

*per R-8201 · produced by T-01 · consumed by: every project; T-02…T-14*

- Produces: the project names above; test projects reference their source project; `Legislator.Parity.Tests` references `Legislator.Cli`.

## C-02 Core abstractions — environment, process, and the test fakes

*per R-8204 · produced by T-02 · consumed by: Engine (T-07…T-10), Hooks (T-11), Cli `Composition` (T-02), every test project*

- Produces:
  ```csharp
  public interface IEnvironment { string? GetVariable(string name); string CurrentDirectory { get; } string HomeDirectory { get; } }
  public sealed record ProcessResult(int ExitCode, string Stdout, string Stderr);
  public interface IProcessRunner { ProcessResult Run(string fileName, IReadOnlyList<string> args, string workingDirectory, TimeSpan timeout); }
  ```
  `System.IO.Abstractions.IFileSystem` and `System.TimeProvider` are used as-is.
- Test helper (in Core.Tests, `public`, reused by every later test project via `InternalsVisibleTo` is *not* used — it is a `TestSupport` project-less folder copied by `<Compile Include>` link in each test csproj):
  ```csharp
  public sealed class FakeEnvironment : IEnvironment { public Dictionary<string,string> Vars {get;} = new(); public string CurrentDirectory {get;set;} = "/work"; public string HomeDirectory {get;set;} = "/fake-home"; public string? GetVariable(string n) => Vars.GetValueOrDefault(n); }
  public sealed class FakeProcessRunner : IProcessRunner { public Func<string, IReadOnlyList<string>, string, ProcessResult> OnRun {get;set;} = (_,_,_) => new(0,"",""); public List<(string,IReadOnlyList<string>,string)> Calls {get;} = new(); public ProcessResult Run(string f, IReadOnlyList<string> a, string d, TimeSpan t){ Calls.Add((f,a,d)); return OnRun(f,a,d);} }
  ```

*Amended 2026-08-31 (T-05, owner ruling):* `IEnvironment` gains `IEnumerable<string> VariableNames` — the environment layer cannot refuse a name it cannot see, so R-8211's "unknown key in any layer" needs enumeration, not lookup. `SystemEnvironment` reads `Environment.GetEnvironmentVariables().Keys`; the fakes move to `tests/Legislator.TestSupport/` as a shared project, since T-06…T-11 give them four more consumers and a test project referencing another test project drags its tests and packages along.

## C-03 The options model — the only home of defaults

*per R-8209, R-8213 · produced by T-03 · consumed by: T-04 composer, T-07…T-10 jobs (`RepoLayout`), `config show` (T-05), BL-083*

- Produces:
  ```csharp
  public enum OptionsLayer { Defaults, Machine, Instance, Environment }
  public sealed record OptionValue<T>(T Value, OptionsLayer Source);
  public sealed class LegislatorOptions
  {
      // every member: a default here and nowhere else. Keys are the YAML/env names.
      public OptionValue<string> DocsDir { get; init; } = new("docs", OptionsLayer.Defaults);               // key: docs_dir
      public OptionValue<string> AiDir { get; init; } = new("ai", OptionsLayer.Defaults);                   // ai_dir     (under docs)
      public OptionValue<string> RulesDir { get; init; } = new("rules", OptionsLayer.Defaults);             // rules_dir  (under docs/ai)
      public OptionValue<string> OkfDir { get; init; } = new("okf", OptionsLayer.Defaults);                 // okf_dir
      public OptionValue<string> CasesDir { get; init; } = new("cases", OptionsLayer.Defaults);             // cases_dir
      public OptionValue<string> AdrDir { get; init; } = new("adr", OptionsLayer.Defaults);                 // adr_dir
      public OptionValue<string> JournalDir { get; init; } = new("journal", OptionsLayer.Defaults);         // journal_dir
      public OptionValue<string> ManifestFile { get; init; } = new("manifest.json", OptionsLayer.Defaults); // manifest_file
      public OptionValue<string> BaselineFile { get; init; } = new("baseline.md", OptionsLayer.Defaults);   // baseline_file
      public OptionValue<string> EntryDocument { get; init; } = new("AGENTS.md", OptionsLayer.Defaults);    // entry_document
      public OptionValue<string> EntryAlias { get; init; } = new("CLAUDE.md", OptionsLayer.Defaults);       // entry_alias
      public OptionValue<string> OpencodeConfig { get; init; } = new("opencode.json", OptionsLayer.Defaults);
      public OptionValue<string> ProjectRulesDir { get; init; } = new(".claude/rules", OptionsLayer.Defaults);
      public OptionValue<string> BacklogFile { get; init; } = new("backlog.md", OptionsLayer.Defaults);
      public OptionValue<string> ChangelogFile { get; init; } = new("CHANGELOG.md", OptionsLayer.Defaults);
      public OptionValue<int> OkfDebtDays { get; init; } = new(30, OptionsLayer.Defaults);                  // okf_debt_days
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
      public static IReadOnlyDictionary<string, string> KeyMap { get; }  // "docs_dir" → nameof(DocsDir) … generated by hand, asserted complete by test
      public IEnumerable<(string Key, string Value, OptionsLayer Source)> Enumerate();
  }
  ```
  The list above is the v24 engine's constant surface (`ROOT/docs/okf`, `docs/cases`, `HUMAN_CLASS`, `BUILD_DIRS`, `SOURCE_EXTS`, `DEBT_DAYS`, the audit checks' file names). Add a member whenever a port in Tasks 6–10 meets another literal — never the literal.

*Amended 2026-08-31 (T-04, owner ruling):* `LegislatorOptions` is a `sealed record` (the composer stamps layers with `with`-expressions); it also carries `public const char ListSeparator = ','` (the one character that joins and splits list options in YAML sequences, env values and `Enumerate()`) and `public static IReadOnlySet<string> IntegerKeys` (the keys whose member is `OptionValue<int>`, asserted against the members by test).

## C-04 Configuration layers, composition, validation, provenance

*per R-8210, R-8211, R-8212 · produced by T-04 · consumed by: Cli `ConfigCommand` (T-05), hurting case HC-8201, BL-083*

- Produces:
  ```csharp
  public sealed record OptionsError(OptionsLayer Layer, string Key, string Reason);
  public sealed class OptionsException(IReadOnlyList<OptionsError> errors) : Exception { public IReadOnlyList<OptionsError> Errors {get;} = errors; }
  public static class OptionsComposer
  {
      // machineFile/instanceFile: absolute paths or null (layer absent). Throws OptionsException (all errors, first layer first).
      public static LegislatorOptions Compose(IFileSystem fs, IEnvironment env, string? machineFile, string? instanceFile);
  }
  public static class YamlLayerReader { public static IReadOnlyDictionary<string,string> Read(IFileSystem fs, string path); } // flat top-level scalars and sequences only
  public static class EnvLayerReader  { public static IReadOnlyDictionary<string,string> Read(IEnvironment env, IEnumerable<string> knownKeys); } // LEGISLATOR_DOCS_DIR → docs_dir
  public static class OptionsValidator { public static IReadOnlyList<OptionsError> Validate(OptionsLayer layer, IReadOnlyDictionary<string,string> raw); } // unknown key; int keys parse ≥ 1; non-empty strings; no path separator '..' segments
  ```
  Precedence low→high: Defaults, Machine, Instance, Environment. A key set in a higher layer overrides and stamps its `Source`.

*Amended 2026-08-31 (T-04, owner ruling):* `YamlLayerReader.Read(IFileSystem fs, string path, OptionsLayer layer)` — the layer is passed so a structural fault (document not a mapping, nested value, invalid YAML) becomes an `OptionsError` of that layer (`Key` empty for document-level faults); the reader throws `OptionsException`, the composer catches per layer and keeps collecting. `EnvLayerReader.Prefix` (`LEGISLATOR_`) is public. Lists from env and from YAML scalars are split on `LegislatorOptions.ListSeparator`; an item containing a comma is not expressible. Only known keys are asked of the environment (`IEnvironment` cannot enumerate), so a stray `LEGISLATOR_*` variable is not an error — open question for T-05. `YamlStaticContext.cs` was not needed (event-stream parser, no deserializer).

*Amended 2026-08-31 (T-05, owner ruling — closes the open question T-04 left):* `EnvLayerReader.Read(IEnvironment env)` — the `knownKeys` parameter is gone. Every `LEGISLATOR_*` variable is admitted as the key its lowercased remainder spells, so a typo reaches the validator and is refused by name (`environment: nope: unknown key`) instead of being silently ignored. Option (b) of the open question; option (a) — document the blindness — was rejected as a written-down defect. The rendering the same question left open: `OptionsException.Message` is `{layer}: {key}: {reason}` per line with the layer lowercased, the `{key}: ` half omitted when the key is empty; `OptionsLayerExtensions.Keyword()` is the single birthplace of a layer's written form, shared by the error lines, `config show`'s `[layer]` stamp and the JSON `source` field.

## C-05 Job contract, CLI entry point, exit codes

*per R-8205, R-8212, R-8214 · produced by T-05 · consumed by: Cli `JobCommand` (T-05), every job (T-07…T-10), parity meta-test (T-06), BL-084 MCP tools (one tool per `IJob`)*

- Produces:
  ```csharp
  public sealed record JobResult(int ExitCode, string Stdout, string Stderr);
  public sealed class JobContext(IFileSystem fs, TimeProvider clock, IEnvironment env, IProcessRunner proc, LegislatorOptions options, string root, IReadOnlyList<string> args, TextWriter? log = null) { /* properties of the same names */ }
  public interface IJob { string Name { get; } string Usage { get; } JobResult Run(JobContext ctx); }
  public static class JobRegistry { public static IReadOnlyDictionary<string, Func<IJob>> Jobs { get; } public static IReadOnlyList<string> Names => Jobs.Keys.Order().ToList(); }
  // Cli
  public static class Program { public static int Main(string[] args); }   // exit codes 0/1/2/3/4 as Global Constraints
  ```
  `legislator <job> [--root <dir>] [job args…]` — `--root` defaults to `env.CurrentDirectory`. `legislator config show [--json] [--root <dir>]`. `legislator version` prints `Version.props`'s value. Unknown job → usage on stderr, exit 2. Any exception escaping a job → `engine failed: <Type>: <message>` on stderr, exit 3 — mirrors the Python `main`.

## C-06 Parity rulers — command overrides and the twin attribute

*per R-8205, R-8206 · produced by T-06 · consumed by: `evals/check_engine.py`, `evals/check_hooks.py`, `tests/Legislator.Parity.Tests` (T-06…T-11), `tools/evals-bg.sh` (T-13)*

**Amendment (2026-09-01, T-07 — operator ruling).** The two variables are renamed OUT of the `LEGISLATOR_*` namespace, to `PARITY_ENGINE_CMD` and `PARITY_HOOK_CMD`. The environment layer admits every `LEGISLATOR_*` name as the option key it spells and refuses an unknown one by name (R-8210, T-05 ruling), so while the binary is the arm its own harness variable made it exit 2 on every check — the ruler measured the collision, not the port. T-06 could not see this: the registry was empty and every job exited 2 anyway, so the fault hid inside the expected failure. Whether an unrecognised `LEGISLATOR_*` variable should be fatal at all is a separate question about the T-05 design and is filed in `docs/backlog.md`.

- `PARITY_ENGINE_CMD` — when set, `run()`/`audit()`/`eng()` execute `[$PARITY_ENGINE_CMD, <job>, "--root", <root>, …]` instead of `python3 docs/ai/engine.py`; the fixture repos still copy `engine.py` in (until Task 12 removes it), so both arms are measured on identical trees.
- `PARITY_HOOK_CMD` — when set, `run_hook(script, …)` executes `[$PARITY_HOOK_CMD, "hook", <script.stem>]`.
- `[Parity("engine", "anchors_clean_repo_exit_0")]` — attribute naming the ruler (`engine`|`hooks`) and label a .NET test twins.

## C-07 Repo layout and the anchors classifier

*per R-8205, R-8204, R-8209 · produced by T-07 · consumed by: T-08 `okf-debt`, T-09 `audit` check 15/17, T-10 `verify`*

- Produces:
  ```csharp
  public sealed class RepoLayout(LegislatorOptions o, string root) { public string Docs {get;} public string Okf {get;} public string Cases {get;} public string Ai {get;} public string Rules {get;} public string Manifest {get;} /* joined with '/' — never Path.Combine on a real disk */ }
  public enum AnchorKind { None, Path, Symbol }
  public static class AnchorClassifier { public static AnchorKind Classify(string token, IReadOnlySet<string> topLevelDirs); public static string PathTarget(string token); /* strips trailing .Member() */ }
  public sealed class SymbolIndex { public static SymbolIndex Build(IFileSystem fs, string root, LegislatorOptions o); public bool Contains(string leadingSegment); }
  public sealed class AnchorsJob : IJob { public string Name => "anchors"; … }
  public static class Findings { public static JobResult AsResult(IEnumerable<string> findings) => new(findings.Any() ? 1 : 0, string.Join("", findings.Order(StringComparer.Ordinal).Select(f => f + "\n")), ""); }
  ```
  Finding text is byte-identical to the Python: `"{rel}:{lineno}: path-anchor: {token} → no such file"` and `"{rel}:{lineno}: symbol-anchor: {token} → not found in {roots}"`, sorted ordinal.

## C-08 Git history and the case model

*per R-8205, R-8209 · produced by T-08 · consumed by: T-08 `okf-debt`/`sdd-lint`/`baseline`, T-09 `audit`*

- Produces:
  ```csharp
  public static class GitLog { /* newest commit ISO date for rel path, or null when untracked / no git; NoGit flag when the git call itself fails */ public static (string? Iso, bool GitAvailable) NewestCommit(IProcessRunner proc, LegislatorOptions o, string root, string rel); }
  public sealed record CaseFile(string Dir, string Spec, string? Plan, string Header, IReadOnlyList<string> RequirementIds, bool Converged);
  public static class CaseModel { public static IReadOnlyList<CaseFile> Load(IFileSystem fs, RepoLayout l); }
  ```
  `okf-debt` **without git is a loud stop** — the BL-069 F1 fix, already in the Python since BL-070; the twin asserts it.

  *(Amended 2026-09-01, T-08 green.* `CaseFile` is `(string Path, string Relative, IReadOnlyDictionary<string, string> Requirements, bool Converged)`, not the `(Dir, Spec, Plan, Header, RequirementIds, Converged)` written here. Two reasons: the baseline job displays each requirement's **definition text**, so a list of ids cannot serve it, and the spec, plan and header are read where they are needed rather than carried — a record holding three document bodies is a cache nobody invalidates. `CaseModel.Documents(fs, file)` walks the case's markdown, and `Prose` in `Legislator.Engine.Sdd` holds the fence/inline-code reading both the model and the lints share.)*

  *(Amended 2026-09-01, T-08 red review.* This contract said "a loud finding, exit 1"; the ruler says otherwise and the ruler is the law here (R-8205). `check_engine.py`'s `okf_debt_git_absent` asserts `returncode not in (0, 1, 2)`, `"git"` in **stderr** and an **empty stdout** — the Python raises, and the host renders exit 3. So the job throws and the CLI's last line of defence prints `engine failed: …` on stderr; nothing a stdout-reader could mistake for a finding is ever written. *A second amendment:* absent git is discovered by the attempt failing, not by a `which` probe — `IProcessRunner` raises `ProcessStartException` when the executable cannot be started, which is the one process failure that is not a result, and `GitLog` turns it into `GitAvailable = false`. A `which` of our own would duplicate the operating system's rules (`PATHEXT`, the execute bit) and would still call a present-but-unrunnable git available. *A third:* `AnchorTarget.Resolve` in `Legislator.Engine.Anchors` is where a path-anchor becomes a repository-relative file, shared by `anchors` and `okf-debt` — the Python's `path_target` probes the disk for a `Type.Member()` stem, so a job that resolved tokens on its own would accrue debt on a different set of files than the ruler measures. `OkfDocuments` in `Legislator.Engine.Okf` holds the anchored class, the relative form and the scannable lines for the same reason.)*

## C-09 `audit` and `detect` command lines

*per R-8205 · produced by T-09 · consumed by: SKILL.md Step 1 / audit section (T-13), `evals/check_engine.py`*

- `legislator audit --skill <path> [--root <dir>] [--model-findings <json>]` — exit 1 when any finding, 0 clean; report on stdout starts `# AI-Layer Audit`, clean report contains `No findings.`.
- `legislator detect --skill <path> [--root <dir>]` — JSON (`indent=1, sort_keys` in Python → `JsonSerializerOptions { WriteIndented = true }` gives 2-space indent: **the twin asserts the parsed object, and the ruler's `detect` checks parse JSON too — confirm by reading them; if any compares raw text, emit with a custom 1-space writer to stay byte-identical**).
- `--skill` missing or not a directory → stderr `"{job} requires --skill <skill-path> (the legislator package root)"`, exit 2 (verbatim).

## C-10 `apply`, `verify`, `report` command lines and the run record

*per R-8205 · produced by T-10 · consumed by: SKILL.md Steps 3/6/7 (T-13), `evals/grade.py` re-print helper*

- `apply --skill <p> --stacks <a,b> [--keep-add <path>::<reason>]* [--keep-remove <path>]* [--record <file>] [--root <dir>]` — stdout lines exactly as `_run_job` prints (`apply: {mode} mode, constitution v{version}, stacks [...]`, the `owned:`/`keep:`/`file model:` lines, `run record: {path}`); exit 4 with `apply stopped: {reason}` on stderr when two real entry documents exist, **having written nothing**; `--keep-add` without `::` → usage, exit 2.
- `verify [--record <file>]` — failures one per line, exit 1; appends the post snapshot to the record.
- `report [--record <file>] [--model-findings <json>]` — the Step-7 report from the record.
- Record path default: `RunRecordDir` under the system temp dir (`ctx.Fs.Path.GetTempPath()` — an `IFileSystem` call, permitted).

## C-11 Hook contract

*per R-8208 · produced by T-11 · consumed by: `plugin/hooks/hooks.json` (T-11), `evals/check_hooks.py`, BL-077 registry predicate*

- `legislator hook <guard_owned_files|guard_git_conduct|format_on_edit|okf_sync_check>` reads one JSON object on stdin; exit `0` allow, `2` block with the message on stderr; **never** another exit code, **never** an exception escaping — malformed input → 0 (the hook contract: a crash must not stop the user's work). `HookCommand` wraps every hook in a catch-all returning 0.
- `hooks.json` commands become `"legislator hook guard_owned_files"` etc. (the binary is on PATH by Task 12's installer); `format_on_edit` keeps its `timeout: 10`.
- The registry predicate (walk up to `docs/ai/manifest.json`) is the v24 one; BL-077 replaces it with the machine registry on this same code.

## C-12 Publish, install, `version --json`, arm integrity

*per R-8203, R-8214, R-8215 · produced by T-12 · consumed by: `evals/check_static.py` edition pin (T-12), README install/runbook (T-14), `evals/benchmarks/v25.md`, audit `arm-integrity` (T-09/T-12)*

- `tools/publish-legislator.sh` → `artifacts/<rid>/legislator[.exe]` + `artifacts/SHA256SUMS` for `linux-x64 win-x64 osx-x64 osx-arm64`.
- `tools/install-legislator.sh [--from artifacts/<rid>]` → copies to `~/.local/bin/legislator` (Linux/macOS) — the operator-side script (declared operator-side-Linux/macOS in the register; the Windows install is `Copy-Item` documented in README, per BL-068's declaration rule).
- `legislator version --json` → `{"version":"25.0.0","rid":"linux-x64","sha256":"…"}`.
- Audit check `arm-integrity`: **absent binary or mismatch is a finding** (verification fails loud); the *hooks* never depend on this — they are the binary.

*Amended 2026-08-31 (T-05, as built):* the job map is a parameter, not a static write target — `Program.Run(string[] args, IReadOnlyDictionary<string, Func<IJob>> jobs, IFileSystem fs, TimeProvider clock, IEnvironment env, IProcessRunner proc, TextWriter stdout, TextWriter stderr)`, with `Main` passing `JobRegistry.Jobs`. The plan's test-only `JobRegistry.Register` is not built: a registry a caller can write into is shared mutable state between tests, and the parameter gives the same reach with none of it. `--root <dir>` is taken out of the argument list wherever it stands and what remains is the job's own `Args`; a `--root` without a directory is usage. `config show` renders `{key} = {value}  [{layer}]` per line and `--json` the same content as `{"<key>":{"value":…,"source":…}}` in `Enumerate()` order, written with `Utf8JsonWriter` rather than a source-generated `JsonSerializerContext` — the shape is fixed and hand-writing it keeps the AOT surface trivial. `version` prints the assembly's informational version, pinned by `src/Legislator.Cli/Version.props` (`IncludeSourceRevisionInInformationalVersion=false`, or the `+<sha>` suffix would break the pinned number). `JobRegistry.Names` is not yet covered by a test — over an empty registry the assertion is vacuous; its red belongs to T-07, the first registration.
